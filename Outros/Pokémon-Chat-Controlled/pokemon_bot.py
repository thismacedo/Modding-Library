import pytchat
import keyboard
import time
import re
import configparser
import logging
from datetime import datetime
import threading
import queue

# Configuração de logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("pokemon_bot.log"),
        logging.StreamHandler()
    ]
)

# Mapeamento de teclas específico para mGBA
key_mapping = {
    'A': 'z',
    'B': 'x',
    'UP': 'up',
    'DOWN': 'down',
    'LEFT': 'left',
    'RIGHT': 'right',
    'SELECT': 'a',
    'START': 's',
    'CIMA': 'up',
    'BAIXO': 'down',
    'ESQUERDA': 'left',
    'DIREITA': 'right'
}

multi_step_commands = ['UP', 'DOWN', 'LEFT', 'RIGHT', 'CIMA', 'BAIXO', 'ESQUERDA', 'DIREITA']

def load_config():
    config = configparser.ConfigParser()
    try:
        config.read('config.ini')
        return {
            'video_id': config['YouTube']['video_id'],
            'delay': float(config['Controls']['command_delay']),
            'command_prefix': config['Controls']['command_prefix'],
            'valid_commands': [cmd.strip().upper() for cmd in config['Controls']['valid_commands'].split(',')],
            'max_steps': int(config['Controls'].get('max_steps', '20'))
        }
    except Exception as e:
        logging.error(f"Erro ao carregar configuração: {e}")
        return {
            'video_id': '',
            'delay': 0.3,
            'command_prefix': '!',
            'valid_commands': ['A', 'B', 'UP', 'DOWN', 'LEFT', 'RIGHT', 'SELECT', 'START', 
                              'CIMA', 'BAIXO', 'ESQUERDA', 'DIREITA'],
            'max_steps': 20
        }

def press_key_once(mapped_key):
    try:
        keyboard.press(mapped_key)
        time.sleep(0.1)
        keyboard.release(mapped_key)
        return True
    except Exception as e:
        logging.error(f"Erro ao pressionar tecla {mapped_key}: {e}")
        return False

def press_key(key, steps=1):
    try:
        key = key.upper()
        if key in key_mapping:
            mapped_key = key_mapping[key]
            if steps > 1:
                logging.info(f"Pressionando tecla {key} ({mapped_key}) {steps} vezes")
                for _ in range(steps):
                    press_key_once(mapped_key)
                    time.sleep(0.1)
            else:
                logging.info(f"Pressionando tecla {key} ({mapped_key})")
                press_key_once(mapped_key)
            return True
        else:
            logging.warning(f"Tecla {key} não mapeada")
            return False
    except Exception as e:
        logging.error(f"Erro ao pressionar tecla {key}: {e}")
        return False

# Função modificada para aceitar apenas comandos com prefixo
def process_message(message, author, command_queue, config):
    message_original = message.strip()
    message = message_original.upper()
    
    logging.debug(f"Mensagem recebida: '{message_original}' de {author}")
    
    valid_commands = config['valid_commands']
    prefix = config['command_prefix']
    max_steps = config['max_steps']
    
    # Verifica se a mensagem começa com o prefixo
    if not message.startswith(prefix):
        logging.debug(f"Mensagem ignorada: sem prefixo '{prefix}'")
        return False
    
    # Remove o prefixo e processa o comando
    cmd_text = message[len(prefix):].strip()
    
    # Verifica se tem espaço (possível número de passos)
    if ' ' in cmd_text:
        parts = cmd_text.split(' ', 1)
        cmd = parts[0].upper()
        if cmd in valid_commands and len(parts) > 1 and parts[1].isdigit():
            steps = min(int(parts[1]), max_steps)
            if cmd in multi_step_commands:
                logging.info(f"Comando com prefixo e passos: {cmd} {steps}")
                command_queue.put((cmd, steps, author))
                return True
    else:
        # Comando simples com prefixo
        cmd = cmd_text.upper()
        if cmd in valid_commands:
            logging.info(f"Comando simples com prefixo: {cmd}")
            command_queue.put((cmd, 1, author))
            return True
    
    logging.debug(f"Comando inválido após prefixo: {cmd_text}")
    return False

def command_processor(command_queue, config):
    while True:
        if not command_queue.empty():
            command, steps, author = command_queue.get()
            logging.info(f"{author} usou o comando: {command} {steps if steps > 1 else ''}")
            press_key(command, steps)
            time.sleep(config['delay'])
        else:
            time.sleep(0.1)

def main():
    config = load_config()
    video_id = config['video_id']
    
    if not video_id:
        video_id = input("Digite o ID do vídeo da live (parte após v= na URL): ")
    
    max_steps = config['max_steps']
    valid_commands = config['valid_commands']
    
    logging.info(f"Bot iniciado para o vídeo ID: {video_id}")
    logging.info(f"Comandos válidos (com prefixo '{config['command_prefix']}'): {valid_commands}")
    logging.info(f"Mapeamento de teclas: {key_mapping}")
    logging.info(f"Comandos multi-passos: {multi_step_commands}")
    logging.info(f"Máximo de passos permitidos: {max_steps}")
    
    command_queue = queue.Queue()
    
    processor_thread = threading.Thread(
        target=command_processor, 
        args=(command_queue, config),
        daemon=True
    )
    processor_thread.start()
    
    chat = pytchat.create(video_id=video_id)
    
    print("Bot está rodando! Pressione Ctrl+C para parar.")
    print("IMPORTANTE: Certifique-se que a janela do mGBA está em foco!")
    print(f"Comandos disponíveis (use o prefixo '{config['command_prefix']}'): {', '.join(valid_commands)}")
    print(f"Comandos multi-passos: {', '.join(multi_step_commands)} (exemplo: {config['command_prefix']}LEFT 5)")
    
    try:
        while chat.is_alive():
            for c in chat.get().sync_items():
                message = c.message.strip()
                author = c.author.name
                process_message(message, author, command_queue, config)
            time.sleep(0.1)
    except KeyboardInterrupt:
        logging.info("Bot encerrado pelo usuário")
    except Exception as e:
        logging.error(f"Erro inesperado: {e}")
        logging.exception("Detalhes do erro:")
    finally:
        chat.terminate()
        logging.info("Bot encerrado")

def create_default_config():
    config = configparser.ConfigParser()
    config['YouTube'] = {'video_id': ''}
    config['Controls'] = {
        'command_delay': '0.3',
        'command_prefix': '!',
        'valid_commands': 'A,B,UP,DOWN,LEFT,RIGHT,SELECT,START,CIMA,BAIXO,ESQUERDA,DIREITA',
        'max_steps': '20'
    }
    config['KeyMapping'] = {
        'A': 'z',
        'B': 'x',
        'UP': 'up',
        'DOWN': 'down',
        'LEFT': 'left',
        'RIGHT': 'right',
        'SELECT': 'a',
        'START': 's',
        'CIMA': 'up',
        'BAIXO': 'down',
        'ESQUERDA': 'left',
        'DIREITA': 'right'
    }
    
    with open('config.ini', 'w') as configfile:
        config.write(configfile)
    print("Arquivo de configuração 'config.ini' criado")

def test_emulator_keys():
    print("Teste de teclas no emulador:")
    print("Pressione Enter para iniciar o teste (certifique-se que o mGBA está em foco)")
    input()
    
    print("Testando comandos simples...")
    for cmd in ['A', 'B', 'UP', 'DOWN', 'LEFT', 'RIGHT', 'SELECT', 'START']:
        print(f"Testando comando '{cmd}'...")
        press_key(cmd)
        time.sleep(1)
    
    test_cmd = "RIGHT"
    test_steps = 3
    print(f"Testando {test_steps} passos com {test_cmd}...")
    press_key(test_cmd, test_steps)
    
    print("Teste concluído. As teclas funcionaram no emulador?")

def manual_command_test():
    command_queue = queue.Queue()
    config = load_config()
    
    processor_thread = threading.Thread(
        target=command_processor, 
        args=(command_queue, config),
        daemon=True
    )
    processor_thread.start()
    
    print("TESTE MANUAL DE COMANDOS")
    print(f"Digite um comando com prefixo '{config['command_prefix']}' (ex: '{config['command_prefix']}A', '{config['command_prefix']}UP 5')")
    print("Digite 'sair' para encerrar o teste")
    
    while True:
        test_input = input("> ")
        if test_input.lower() == 'sair':
            break
        result = process_message(test_input, "TESTE", command_queue, config)
        if result:
            print(f"Comando reconhecido e processado!")
        else:
            print(f"Comando não reconhecido (use o prefixo '{config['command_prefix']}')")
        time.sleep(0.5)

if __name__ == "__main__":
    try:
        with open('config.ini', 'r'):
            pass
    except FileNotFoundError:
        create_default_config()
    
    print("Escolha uma opção:")
    print("1. Iniciar bot")
    print("2. Testar teclas no emulador")
    print("3. Teste manual de comandos")
    
    choice = input("Digite o número da opção: ")
    
    if choice == '1':
        main()
    elif choice == '2':
        test_emulator_keys()
    elif choice == '3':
        manual_command_test()
    else:
        print("Opção inválida")