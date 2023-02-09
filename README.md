# Alertegelee.fr (frosto)

Le service alerte gel météo vous envoie gratuitement une notification si des températures négatives sont prévues dans les jours suivants.

https://www.alertegelee.fr/

## Contact

Ce service open source est maintenu par des bénévoles.

Pour nous contacter, envoyez un courrier électronique à l'adresse suivante: alertegelee (arobase) outlook.fr

## Structure

The *Frost Alert* application is aimed to provide notifications when a frost episode is due in the following days.

The application consists in the following components:
- **site**: landing page with subscription form
- **api**: An _Azure Function App_ that receives the subscription form data, fetches weather forecasts and sends notifications.

[Frosto website (in french)](https://www.alertegelee.fr/)

## Licensing

This open source software is distributed under MIT license, please refer to [LICENSE](LICENSE) file.

### Third party licenses

This project uses open-source, third party software:

- [ViteJS](https://github.com/vitejs/vite): MIT License, Copyright (c) 2019-present Evan You & Vite Contributors
- [Bootstrap](https://github.com/twbs/bootstrap): MIT License, Copyright (c) 2011-2022 Twitter, Inc., Copyright (c) 2011-2022 The Bootstrap Authors
- [.NET SDK](https://github.com/dotnet/sdk): MIT License, Copyright (c) .NET Foundation
- [Azure Function Core Tools](https://github.com/Azure/azure-functions-core-tools): MIT License, Copyright (c) .NET Foundation
- [Azurite](https://github.com/Azure/Azurite): MIT License, Copyright (c) Microsoft Corporation

This project uses graphics under _open_ or _permissive_ licences:

- Illustrations by [unDraw](https://undraw.co/license)
- Fav icon by [Oh Rian](https://thenounproject.com/ohrianid/): Creative Commons license  (CCBY)
