import { platform } from "os";
import { spawnCommand } from "../utils/shell";
import path from "path";

export class SigningService {
   
    private static readonly macCertificate = {
        commonName: 'Developer ID Application: Creativeforce.io, Inc. (Y5K3N5Y6PY)',
        entitlementsPath: path.join(
            __dirname,
            '../../../../resources/mac-netcore.entitlements.plist'
          )
    }


    
    static async signFiles(files: string[], _platform = platform()) {
       try {
        console.log(`[SigningService][${_platform}] - Signing files: ${files}`);

        for (const file of files) {
            let code = 0;
           switch (_platform) {
            case 'win32':
               console.log(`[SigningService][${_platform}] - Signing file: ${file}`);
                break;
            case 'darwin':
                code = await this.signApplicaionFileMacOS(file);

                break;
            default:
                break;
           }
           if (code !== 0) {
            throw new Error(`[SigningService][${_platform}] - Signing file: ${file} failed with code ${code}`);
           }
           console.log(`[SigningService][${_platform}] - Signing file: ${file} - result: ${code} ✅`);
           return true;
        }
       } catch (error) {
        console.error(error);
        return false;
       }
    }

    static async signApplicaionFileMacOS(file: string) {
        try {
           const result = await spawnCommand('codesign', [`--force --timestamp --options=runtime --entitlements "${this.macCertificate.entitlementsPath}" --sign "${this.macCertificate.commonName}" "${file}"`]);
           console.log(`[SigningService] Signing mac application file: ${file} - result: ${result}`);
           return result;
        } catch (error) {
            console.error(error);
            return -1;
        }
    }
}