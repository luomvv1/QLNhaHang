import * as signalR from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.listeners = new Map();
    this.isStarting = false;
  }

  getConnection() {
    if (!this.connection) {
      const hubUrl = 'http://localhost:5079/hubs/restaurant';
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => localStorage.getItem('token') || '',
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      this.connection.onreconnecting(error => {
        console.warn('SignalR reconnecting:', error);
      });

      this.connection.onreconnected(connectionId => {
        console.log('SignalR reconnected successfully:', connectionId);
      });

      this.connection.onclose(error => {
        console.warn('SignalR connection closed:', error);
      });
    }
    return this.connection;
  }

  async start() {
    const conn = this.getConnection();
    if (conn.state === signalR.HubConnectionState.Connected) {
      return conn;
    }
    if (this.isStarting) {
      return conn;
    }

    this.isStarting = true;
    try {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        await conn.start();
        console.log('SignalR WebSocket connected to RestaurantHub!');
      }
    } catch (err) {
      console.warn('SignalR initial connection failed, will retry:', err);
    } finally {
      this.isStarting = false;
    }
    return conn;
  }

  on(eventName, callback) {
    const conn = this.getConnection();
    conn.on(eventName, callback);
    this.start().catch(() => {});
  }

  off(eventName, callback) {
    if (this.connection) {
      if (callback) {
        this.connection.off(eventName, callback);
      } else {
        this.connection.off(eventName);
      }
    }
  }
}

const service = new SignalRService();
export const signalrService = service;
export default service;