import { v4 } from 'uuid';
import { WebSocketServer, Server } from 'ws';
import * as WS from 'ws';
import { CFAppSocketProvider, CFAppSocketEvent, CFAppSocketSetting, CFAppSocketClient } from './types';

export class CFAppSocketServer {
  readonly setting: CFAppSocketSetting;
  private readonly serverIdentifier: string;
  private ws!: Server;
  private clients: CFAppSocketClient[] = [];
  private registeredEvents: Map<string, (data: any) => void> = new Map();
  private onClientConnectedListeners: Map<string, () => void> = new Map();
  private eventQueue: Array<CFAppSocketEvent> = [];
  private isConsuming = false;
  private _provider: CFAppSocketProvider;
  constructor(provider: CFAppSocketProvider) {
    this.serverIdentifier = provider.serverIdentifier;
    this.setting = provider.setting;
    this._provider = provider;
  }

  initSocket(ready?: () => void) {
    this.ws = new WebSocketServer({
      host: this.setting.host,
      port: this.setting.port,
      // noServer: true,
    });
    this.ws.on('error', (err) => {
      this.log('error', `[LOCAL-WS] - [${this.serverIdentifier}] Socket server error`, err);
      return setTimeout(() => {
        this.initSocket(ready);
      }, 1000);
    });
    this.ws.on('listening', () => {
      if (ready) ready();
    });
    this.ws.on('connection', (wsclient) => {
      const randomId = v4();
      this.log('info', `[LOCAL-WS] - [${this.serverIdentifier}] A client connected with id: ${randomId}`);
      wsclient.on('message', (data: string) => {
        this.handleSocketClientMessages(randomId, wsclient, data);
      });
      wsclient.on('close', (code, reason) => {
        const registeredClient = this.clients.find((t) => t.wsId === randomId);
        this.log(
          'warn',
          `[LOCAL-WS] - [${this.serverIdentifier}] [${randomId}|${
            registeredClient ? registeredClient.chanel : 'no-registered-chanel'
          }] Client closed code:${code}, reason: ${reason}`
        );
      });
      wsclient.on('error', (err) => {
        const registeredClient = this.clients.find((t) => t.wsId === randomId);
        this.log(
          'warn',
          `[LOCAL-WS] - [${this.serverIdentifier}] [${randomId}|${
            registeredClient ? registeredClient.chanel : 'no-registered-chanel'
          }] Client error`,
          err
        );
      });
      setTimeout(() => {
        if (wsclient && wsclient.readyState === WS.OPEN) {
          if (this.clients.findIndex((t) => t.wsId === randomId) < 0) {
            try {
              this.log(
                'warn',
                `[LOCAL-WS] - [${this.serverIdentifier}] Timeout to register client ${randomId}, force close connected client`
              );
              wsclient.close(4999, 'Close due timeout to register wsclient');
            } catch (err) {
              this.log(
                'warn',
                `[LOCAL-WS] - [${this.serverIdentifier}] Cannot close not register client ${randomId}`,
                err
              );
            }
          }
        }
      }, 60000);
    });
  }

  send(chanel: string, event: string, data: any) {
    this.eventQueue.push({
      chanel: chanel,
      event: event,
      body: data,
    });
    setTimeout(() => {
      this.consumeEventQueue();
    });
  }
  listen(chanel: string, event: string, callback: (data: any) => void) {
    this.registeredEvents.set(`${chanel}::${event}`, callback);
  }
  close() {
    this.log('warn', `[LOCAL-WS] - [${this.serverIdentifier}] Socket server will close`);
    for (let i = 0; i < this.clients.length; i++) {
      if (this.clients[i].ws && this.clients[i].ws.readyState === WS.OPEN) {
        this.clients[i].ws.close();
      }
    }
    this.clients = [];
    this.ws.close();
  }
  totalClientInChanel(chanel: string) {
    return this.clients.filter((t) => t.chanel === chanel).length;
  }
  getClientsInformation(chanel?: string) {
    return (chanel ? this.clients.filter((t) => t.chanel === chanel) : this.clients).map((c) => {
      return {
        chanel: c.chanel,
        processName: c.processName,
        processId: c.processId,
      };
    });
  }
  onClientConnected(chanel: string, listener: () => void) {
    this.onClientConnectedListeners.set(chanel, listener);
  }
  removeClient(chanel: string) {
    this.log('warn', `[LOCAL-WS] - [${this.serverIdentifier}] [${chanel}] Client removed`);
    const closedClientIndex = this.clients.findIndex((t) => t.chanel === chanel);
    if (closedClientIndex >= 0) {
      const client = this.clients.splice(closedClientIndex, 1);
      if (client[0].ws && client[0].ws.readyState === WS.OPEN) {
        client[0].ws.close();
      }
    }
  }

  private handleSocketClientMessages(wsId: string, wsClient: WS, message: string) {
    const wsData: CFAppSocketEvent = JSON.parse(message);
    if (!wsData || !wsData.event) {
      return;
    }
    if (wsData.event === 'register') {
      return this.handleClientRegister(wsId, wsClient, wsData);
    }
    return this.handleClientEvent(wsData);
  }
  private handleClientRegister(wsId: string, wsClient: WS, wsData: CFAppSocketEvent) {
    if (this.clients.findIndex((t) => t.chanel === wsData.chanel) >= 0) {
      this.log(
        'warn',
        `[LOCAL-WS] - [${this.serverIdentifier}] [${wsData.chanel}] Already existed a client of chanel ${wsData.chanel}`,
        this.clients.find((t) => t.chanel === wsData.chanel)
      );
      setTimeout(() => {
        try {
          if (wsClient.readyState === WS.OPEN) {
            wsClient.close(4999, 'Close socket due duplication client');
          }
        } catch (err) {
          this.log(
            'warn',
            `[LOCAL-WS] - [${this.serverIdentifier}] [${wsData.chanel}] Cannot try to close duplicate client`,
            err
          );
        }
      }, 5000);
    } else if (wsData.body && wsData.body.processId && wsData.body.processName) {
      this.log(
        'info',
        `[LOCAL-WS] - [${this.serverIdentifier}] [${wsData.chanel}] [${wsId}] Client registered with sessionId: ${wsData.body.sessionId}`
      );
      this.clients.push({
        chanel: wsData.chanel,
        ws: wsClient,
        wsId: wsId,
        processId: wsData.body.processId,
        processName: wsData.body.processName,
        sessionId: wsData.body.sessionId,
      });
      wsClient.on('close', (code, reason) => {
        this.log(
          'warn',
          `[LOCAL-WS] - [${this.serverIdentifier}] [${wsData.chanel}] [${wsId}] Client closed: ${code} - ${reason}`
        );
        const closedClientIndex = this.clients.findIndex((t) => t.chanel === wsData.chanel);
        if (closedClientIndex >= 0) {
          this.clients.splice(closedClientIndex, 1);
        }
      });
      wsClient.on('error', (err) => {
        this.log('warn', `[LOCAL-WS] - [${this.serverIdentifier}] [${wsData.chanel}] Client error`, err);
      });
      const listener = this.onClientConnectedListeners.get(wsData.chanel);
      if (listener) {
        listener();
      }
    }
  }
  private handleClientEvent(wsData: CFAppSocketEvent) {
    if (this.clients.findIndex((t) => t.chanel === wsData.chanel) < 0) {
      return;
    }
    const handler = this.registeredEvents.get(`${wsData.chanel}::${wsData.event}`);
    if (handler) {
      handler(wsData.body);
    }
  }
  private consumeEventQueue() {
    if (this.isConsuming) {
      return;
    }
    if (this.ws && this.ws.clients && this.ws.clients.size > 0) {
      this.isConsuming = true;
      const itemsLeft: Array<CFAppSocketEvent> = [];
      while (this.eventQueue.length > 0) {
        const consumeEvent = this.eventQueue.pop();
        if (!consumeEvent) {
          continue;
        }
        const registeredClient = this.clients.find((t) => t.chanel === consumeEvent.chanel);
        if (!registeredClient || !registeredClient.ws) {
          itemsLeft.push(consumeEvent);
        } else {
          registeredClient.ws.send(JSON.stringify(consumeEvent), (err) => {
            if (err) {
              this.log(
                'error',
                `[LOCAL-WS] - [${this.serverIdentifier}] [${consumeEvent.chanel}] Send socket event ${consumeEvent.event} error`,
                err
              );
            }
          });
        }
      }
      this.eventQueue.push(...itemsLeft);
      this.isConsuming = false;
    }
    if (this.eventQueue.length > 0) {
      return setTimeout(() => {
        return this.consumeEventQueue();
      }, 1000);
    }
  }

  private log(level: 'info' | 'warn' | 'error', message: string, ...args: any[]) {
    console[level](message, ...args);
  }
}
