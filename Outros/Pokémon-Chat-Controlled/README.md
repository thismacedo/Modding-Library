**Pokemon Chat-Controlled**
Um programa em Python que permite controlar um emulador mGBA (Game Boy Advance) através de comandos enviados no chat de uma live do YouTube. Desenvolvido para interatividade em streams, o programa lê mensagens do chat em tempo real e traduz comandos prefixados (ex.: !A, !UP 5) em ações no jogo Pokémon ou outros títulos compatíveis.

**Recursos**

- Controle via Chat: Suporta comandos como !A, !B, !UP, !DOWN, com opção de múltiplos passos (ex.: !LEFT 5).
- Configuração Flexível: Ajuste o prefixo, delay e mapeamento de teclas via config.ini.
- Logging Detalhado: Registra todas as ações e erros em um arquivo de log (pokemon_bot.log).
- Testes Integrados: Inclui modos de teste para teclas do emulador e comandos manuais.
- Multithreading: Processamento assíncrono de comandos para evitar travamentos.

**Requisitos**

- Python 3.x
- Bibliotecas: pytchat, keyboard, configparser
- Emulador mGBA com janela em foco

**Como Usar**

- Configure o config.ini com o ID do vídeo da live.
- Instale as dependências: pip install -r requirements.txt.
- Execute: python pokemon_bot.py e selecione uma opção (bot, teste de teclas ou comandos manuais).

Ideal para streamers que querem engajar a audiência em jogatinas interativas! Contribuições são bem-vindas.

