import { CFAppError } from '../common/cf-app-error';

export const ERROR_CODES = {
    SOCKET: {
      CONFIG_NOT_FOUND: 'SOCKET_CONFIG_NOT_FOUND',
      SERVER_NOT_FOUND: 'SOCKET_SERVER_NOT_FOUND',
      SERVER_ALREADY_INITIALIZED: 'SOCKET_SERVER_ALREADY_INITIALIZED',
      CANNOT_INIT_SERVER: 'SOCKET_CANNOT_INIT_SERVER',
      INVALID_CONFIG: 'SOCKET_INVALID_CONFIG',
      TIMEOUT: 'SOCKET_TIMEOUT',
      INIT_ERROR: 'SOCKET_INIT_ERROR',
    },
  };
  
  export type SocketErrorCode = keyof typeof ERROR_CODES.SOCKET;
  


// Define socket errors
export class SocketError extends CFAppError {
  constructor(keyCode: SocketErrorCode, message: string, traceId?: string) {
    super(ERROR_CODES.SOCKET[keyCode], message, traceId);
  }
}

export class SocketConfigNotFoundError extends SocketError {
  constructor(traceId?: string) {
    super('CONFIG_NOT_FOUND', 'Socket config not found', traceId);
  }
}

export class SocketServerAlreadyInitializedError extends SocketError {
  constructor(serverIdentifier: string, traceId?: string) {
    super('SERVER_ALREADY_INITIALIZED', `Socket server ${serverIdentifier} already initialized`, traceId);
  }
}

export class SocketCannotInitServerError extends SocketError {
  constructor(message: string, traceId?: string) {
    super('CANNOT_INIT_SERVER', message, traceId);
  }
}

export class SocketInvalidConfigError extends SocketError {
  constructor(message: string, traceId?: string) {
    super('INVALID_CONFIG', message, traceId);
  }
}

export class SocketTimeoutError extends SocketError {
  constructor(serverIdentifier: string, timeout: number, traceId?: string) {
    super('TIMEOUT', `Cannot initialize socket server ${serverIdentifier}: Timeout after ${timeout}ms`, traceId);
  }
}

export class SocketInitError extends SocketError {
  constructor(message: string, traceId?: string) {
    super('INIT_ERROR', message, traceId);
  }
}
