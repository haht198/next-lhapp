import { CFAppSocketServer } from './socket-server';
import { CFAppSocketProvider } from './types';
import {
  SocketServerAlreadyInitializedError,
  SocketInvalidConfigError,
  SocketTimeoutError,
  SocketInitError,
} from './error';

export class CFAppSocket {
  private static servers: Map<string, CFAppSocketServer> = new Map();
  private static _provider: CFAppSocketProvider;
  private static readonly INIT_TIMEOUT = 30000; // 30 seconds timeout
  private static INIT_TIMEOUT_ID: NodeJS.Timeout | null = null;
  /**
   * Khởi tạo một socket server mới
   * @param provider - Thông tin cấu hình cho socket server
   * @returns Promise<CFAppSocketServer> - Promise trả về instance của socket server
   * @throws SocketServerAlreadyInitializedError - Khi server đã tồn tại
   * @throws SocketInvalidConfigError - Khi cấu hình không hợp lệ
   * @throws SocketTimeoutError - Khi khởi tạo timeout
   * @throws SocketInitError - Khi có lỗi khác trong quá trình khởi tạo
   */
  static async start(provider: CFAppSocketProvider): Promise<CFAppSocketServer> {
    try {
      // Validate provider
      if (!provider) {
        throw new SocketInvalidConfigError('Provider is required');
      }

      const { serverIdentifier, setting } = provider;

      // Validate server identifier
      if (!serverIdentifier) {
        throw new SocketInvalidConfigError('Server identifier is required');
      }

      // Check if server already exists
      if (CFAppSocket.servers.get(serverIdentifier)) {
        throw new SocketServerAlreadyInitializedError(serverIdentifier);
      }

      // Validate socket settings
      CFAppSocket.validateSocketSettings(setting);

      // Store provider
      CFAppSocket._provider = provider;
      return await new Promise<CFAppSocketServer>((resolve, reject) => {
        const socketServer = new CFAppSocketServer(provider);
        let socketInitialized = false;
        CFAppSocket.INIT_TIMEOUT_ID = setTimeout(() => {
          if (!socketInitialized) {
            // Cleanup socket server
            socketServer?.close();
            reject(new SocketTimeoutError(serverIdentifier, CFAppSocket.INIT_TIMEOUT));
          }
        }, CFAppSocket.INIT_TIMEOUT);

        // Initialize socket
        socketServer.initSocket(() => {
          CFAppSocket.clearInitSocketTimeout();
          CFAppSocket.servers.set(serverIdentifier, socketServer);
          CFAppSocket.log('info', `[LOCAL-WS] Socket server ${serverIdentifier} was created`, setting);
          socketInitialized = true;
          resolve(socketServer);
        });
      });
    } catch (error: unknown) {
      // Clear timeout
      CFAppSocket.clearInitSocketTimeout();
      if (
        error instanceof SocketServerAlreadyInitializedError ||
        error instanceof SocketInvalidConfigError ||
        error instanceof SocketTimeoutError
      ) {
        throw error;
      }
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      CFAppSocket.log('error', `Failed to start socket server: ${errorMessage}`);
      throw new SocketInitError(errorMessage);
    }
  }

  /**
   * Validate socket settings
   * @param setting - Socket settings to validate
   * @throws SocketInvalidConfigError - If settings are invalid
   */
  private static validateSocketSettings(setting: any): void {
    if (!setting) {
      throw new SocketInvalidConfigError('Socket settings are required');
    }

    if (!setting.host) {
      throw new SocketInvalidConfigError('Socket host is required');
    }

    if (!setting.port) {
      throw new SocketInvalidConfigError('Socket port is required');
    }

    if (typeof setting.port !== 'number' || setting.port <= 0) {
      throw new SocketInvalidConfigError('Socket port must be a positive number');
    }
  }

  static getServer(identifier: string): CFAppSocketServer | undefined {
    return CFAppSocket.servers.get(identifier);
  }

  private static clearInitSocketTimeout(): void {
    if (CFAppSocket.INIT_TIMEOUT_ID) {
      clearTimeout(CFAppSocket.INIT_TIMEOUT_ID);
      CFAppSocket.INIT_TIMEOUT_ID = null;
    }
  }

  private static log(level: 'info' | 'warn' | 'error', message: string, ...args: any[]): void {
    // Add 'CFAppSocket' to the message
    console[level](`[CFAppSocket] ${message}`, ...args);
  }
}
