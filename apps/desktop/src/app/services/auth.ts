import { AuthClientElectron, Configuration } from '@creative-force/app-auth';
import Logger from '@creative-force/eslogger'
import { shell } from 'electron';

export class AuthService {

    private static authClient: AuthClientElectron | null = null;

    static bootstrap(client: 'hue' | 'luma' | 'ink') {
        const config: Configuration = {
            env: 'dev',
            userAgent: client.toUpperCase(),
            openBrowser: (url: string) => {
                shell.openExternal(url);
            },
            clientId: client,
            applicationUrl: `${client}://open`,
            logger: Logger,
        };
        this.authClient = new AuthClientElectron(config);
        Logger.info(`AuthService - bootstrap - ${client} successfully initialized`);
    }

    static async login() {
        if (!this.authClient) {
            throw new Error('Auth client not initialized');
        }
        try {
            const result = await this.authClient.loginViaBrowser();
            return result;
        } catch (error) {
            Logger.error('AuthService - login', error);
            return {
                success: false,
                error: error.message,
            };
        } finally {
            setTimeout(() => {
                this.authClient.onLoginSuccess();
            }, 5000);
        }
    }
}
        

