# PTRON Mobile

App Android/iOS (Expo / React Native) que consome a API REST do PTRON.

## Requisitos

- Node.js 18+
- Expo Go no celular **ou** emulador Android
- Servidor PTRON rodando com a API (`dotnet run --project src/PTRON`)

## Como executar

```bash
cd mobile
npm install
npm start
```

Depois:

- Escaneie o QR Code com o **Expo Go**, ou
- Pressione `a` para abrir no emulador Android

## Conexão com a API

Na tela inicial → **Configurar**:

| Ambiente | URL base sugerida |
|----------|-------------------|
| Emulador Android | `http://10.0.2.2:5083` |
| Celular (mesma Wi‑Fi) | `http://IP-DO-PC:5083` |

Token padrão: `ptron-dev-token-8f4c2a91` (mesmo de `Api:Token` no `appsettings.json`).

Para celular físico, suba o servidor com perfil LAN:

```bash
dotnet run --project src/PTRON --launch-profile PTRON-LAN
```

## Funcionalidades

- Teste de conexão (`GET /api`)
- Tipos, Insumos, Equipamentos (BOM), Entradas, Produção, Produtos
- Token Bearer em todas as chamadas `/api`
- URL e token salvos no aparelho (AsyncStorage)

## Observações

- Tráfego HTTP (não HTTPS) está liberado no Android (`usesCleartextTraffic`) para desenvolvimento na rede local.
- Upload de foto pelo app ainda não está na UI; o campo `fotoPath` pode ser preenchido pela API/web.
