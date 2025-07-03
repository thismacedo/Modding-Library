# AccountEditorVxaos

Editor de contas para servidores VXA-OS, desenvolvido em C# com Windows Forms.

## Requisitos
- .NET 9.0 Desktop Runtime (Windows)
- Banco de dados PostgreSQL acessível

## Configuração
Antes de rodar o programa, crie um arquivo chamado `configs.ini` na mesma pasta do executável (`bin/Release/net9.0-windows/win-x64/` ou `publish/`). O conteúdo deve ser semelhante ao exemplo abaixo:

```
DATABASE_HOST = 127.0.0.1
DATABASE_USER = seu_usuario
DATABASE_PASS = sua_senha
DATABASE_NAME = nome_do_banco
DATABASE_PORT = 5432
```

## Como rodar
1. Compile o projeto usando o Visual Studio 2022+, VSCODE ou via linha de comando:
   ```
   dotnet build -c Release
   ```
2. Navegue até a pasta `bin/Release/net9.0-windows/win-x64/` (ou `publish/` se usar `dotnet publish`).
3. Certifique-se de que o arquivo `configs.ini` está presente e configurado corretamente.
4. Execute o programa clicando duas vezes em `AccountEditorVxaos.exe` ou via terminal:
   ```
   ./AccountEditorVxaos.exe
   ```

## Observações
- O programa utiliza a biblioteca [Npgsql](https://www.npgsql.org/) para conectar ao PostgreSQL.
- Certifique-se de que o banco de dados está acessível e que as credenciais estão corretas.
- Caso encontre erros de dependências, instale o .NET Desktop Runtime 9.0 para Windows. 