import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from './api';
import { NotificationMessageItem } from '../types';

type NotificationHandler = (notification: NotificationMessageItem) => void;
type StatusHandler = (status: 'connected' | 'connecting' | 'disconnected' | 'reconnecting') => void;

class SignalRNotificationService {
  private connection: signalR.HubConnection | null = null;
  private notificationListeners: Set<NotificationHandler> = new Set();
  private statusListeners: Set<StatusHandler> = new Set();
  private status: 'connected' | 'connecting' | 'disconnected' | 'reconnecting' = 'disconnected';

  public startConnection(): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/notifications`, {
        withCredentials: true,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ReceiveNotification', (notification: NotificationMessageItem) => {
      this.notificationListeners.forEach((handler) => handler(notification));
    });

    this.connection.onreconnecting(() => {
      this.setStatus('reconnecting');
    });

    this.connection.onreconnected(() => {
      this.setStatus('connected');
    });

    this.connection.onclose(() => {
      this.setStatus('disconnected');
    });

    this.setStatus('connecting');
    this.connection
      .start()
      .then(() => {
        this.setStatus('connected');
      })
      .catch(() => {
        this.setStatus('disconnected');
      });
  }

  public stopConnection(): void {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
      this.setStatus('disconnected');
    }
  }

  public onNotification(handler: NotificationHandler): () => void {
    this.notificationListeners.add(handler);
    return () => {
      this.notificationListeners.delete(handler);
    };
  }

  public onStatusChange(handler: StatusHandler): () => void {
    this.statusListeners.add(handler);
    handler(this.status);
    return () => {
      this.statusListeners.delete(handler);
    };
  }

  private setStatus(status: 'connected' | 'connecting' | 'disconnected' | 'reconnecting') {
    this.status = status;
    this.statusListeners.forEach((handler) => handler(status));
  }
}

export const signalRService = new SignalRNotificationService();
