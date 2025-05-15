import * as WS from 'ws';

export interface CFAppSocketProvider {
  serverIdentifier: string;
  setting: CFAppSocketSetting;
}

export interface CFAppSocketEvent {
  chanel: string;
  event: string;
  body: any;
}

export interface CFAppSocketSetting {
  host: string;
  port: number;
}

export interface CFAppSocketClient {
  chanel: string;
  processId: string;
  processName: string;
  wsId: string;
  ws: WS;
  sessionId?: string;
}
