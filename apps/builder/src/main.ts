import { NetServicesBuilder } from "./net-services";
import path from "path";
const builder = new NetServicesBuilder({
    services: [
        {
            name: 'Creative Force Services',
            source: path.join(__dirname, '../../../../../../netcore-services/Common.Services'),
            executeFile: 'Common.Services'
        }
    ],
    deployDir: path.join(__dirname, '../../../published-services')
});

builder.start();