# Sistema de Regeneração de HP de Inimigos

Este documento detalha como instalar, configurar e entender o sistema de regeneração de HP de inimigos para o Vxaos.

---

## Visão Geral

Este sistema permite que inimigos do mapa recuperem HP gradualmente após um período sem receber dano, totalmente configurável via o arquivo `configs.ini`. O sistema é independente para cada inimigo e não interfere em outras mecânicas do jogo.

O sistema é opcional, mas válido para quem busca uma lógica que impeça jogadores de nível baixo de derrotarem um monstro de nível alto e obterem muita experiência. A ideia aqui é tornar o jogo mais difícil, mesmo que um pouco.

---

## Passo a Passo para Instalação

### 1. Atualize o arquivo `configs.ini`
Adicione as seguintes linhas ao final do arquivo:

```ini
# Hiwatari - Configurações de regeneração de HP de inimigos
# Tempo (em segundos) que o inimigo deve ficar sem receber dano para começar a regenerar HP
ENEMY_RECOVER_DELAY = 10

# Intervalo periódico (em segundos) para o inimigo recuperar HP
ENEMY_RECOVER_TIME = 3

# Quantidade de HP a ser recuperada pelo inimigo
# Fórmula: Proporcional à agilidade: (ENEMY_RECOVER_HP * agi / 100.0).to_i (mínimo 1)
ENEMY_RECOVER_HP = 50
```

### 2. Modifique os arquivos do servidor

#### a) `Servidor/Map/game_event.rb`
Nos métodos `clear_enemy` e `setup_enemy_settings`, adicione as variáveis de controle:

```ruby
# ... dentro de clear_enemy
@last_damage_time = Time.now # Hiwatari - Controle de dano
@enemy_recover_time = Time.now + ENEMY_RECOVER_DELAY # Hiwatari - Controle de regeneração
# ...

# ... dentro de setup_enemy_settings
@last_damage_time = Time.now # Hiwatari - Inicializa controle de dano
@enemy_recover_time = Time.now + ENEMY_RECOVER_DELAY # Hiwatari - Inicializa controle de regeneração
# ...
```

#### b) `Servidor/Combat/game_enemy.rb`
Adicione o método de regeneração e altere o método `update_enemy`:

```ruby
def update_enemy
  update_st_bf_timers unless dead? #LM² 
  recover_enemy_hp unless dead? # Hiwatari - Regeneração
  if in_battle?
    make_actions
  elsif dead? && Time.now > @revive_time
    revive
  end
end

# Hiwatari - Método de regeneração de HP do inimigo
def recover_enemy_hp
  return if @hp >= mhp # Não regenera se já estiver com HP máximo
  return if Time.now < @enemy_recover_time # Aguarda o intervalo de regeneração
  return if Time.now < @last_damage_time + ENEMY_RECOVER_DELAY # Aguarda o atraso após receber dano

  # Calcula a regeneração proporcional à agilidade do inimigo
  hp_recovery = (ENEMY_RECOVER_HP * agi / 100.0).to_i
  hp_recovery = 1 if hp_recovery < 1 # Garante pelo menos 1 de HP recuperado

  # Aplica a regeneração
  self.hp = [@hp + hp_recovery, mhp].min

  # Atualiza o próximo tempo de regeneração
  @enemy_recover_time = Time.now + ENEMY_RECOVER_TIME

  # Envia atualização de HP para os clientes
  $network.send_enemy_vitals(self)
end
```

#### c) `Servidor/Combat/game_battle.rb`
No método `execute_hp_damage`, acima de `refresh` adicione:

```ruby
# ...
@last_damage_time = Time.now if self.is_a?(Game_Event) && self.enemy? # Hiwatari
# ...
```

#### d) `Servidor/Network/send_data.rb`
Adicione o método para enviar os dados de HP do inimigo:

```ruby
# Hiwatari - Envia atualização de HP do inimigo
def send_enemy_vitals(event)
  return if @maps[event.map_id].zero_players?
  buffer = Buffer_Writer.new
  buffer.write_byte(Enums::Packet::ENEMY_VITALS)
  buffer.write_short(event.id)
  buffer.write_int(event.hp)
  buffer.write_int(event.mp)
  send_data_to_map(event.map_id, buffer.to_s)
end
```

---

### 3. Modifique os arquivos do cliente

#### a) `Cliente/[VS] Enums.rb`
Adicione o pacote `ENEMY_VITALS` à enumeração de pacotes:

```ruby
Packet = enum %w(
  ...
  ENEMY_REVIVE
  ENEMY_VITALS 
  ...
)
```

#### b) `Cliente/[VS] Handle_Data.rb`
Adicione o handler para o novo pacote:

```ruby
when Enums::Packet::ENEMY_VITALS
  handle_enemy_vitals(buffer)
```

E implemente o método:

```ruby
def handle_enemy_vitals(buffer)
  event_id = buffer.read_short
  hp = buffer.read_int
  mp = buffer.read_int
  event = $game_map.events[event_id]
  return unless event && event.actor
  hp_diff = hp - event.actor.hp
  mp_diff = mp - event.actor.mp
  event.actor.hp = hp
  event.actor.mp = mp
  if $game_map.in_screen?(event)
    event.change_damage(hp_diff, mp_diff) if hp_diff != 0 || mp_diff != 0
  end
  event.erase if event.actor.dead?
  $windows[:target_hud].refresh if event == $game_player.target && event.actor?
end
```

---

## Como Funciona

- O inimigo só começa a regenerar HP após ficar `ENEMY_RECOVER_DELAY` segundos sem receber dano.
- A cada `ENEMY_RECOVER_TIME` segundos, se não for atingido, ele regenera HP.
- O valor regenerado é **proporcional à agilidade**: `(ENEMY_RECOVER_HP * agi / 100.0).to_i` (mínimo 1).
- O HP nunca excederá o máximo do inimigo.
- O sistema é totalmente passivo e transparente para o jogador.

---

## Notas e Dicas
- Todos os parâmetros podem ser ajustados no `configs.ini`.
- O sistema é modular e não interfere em outras mecânicas.

---

## Exemplo de Configuração

```ini
ENEMY_RECOVER_DELAY = 8
ENEMY_RECOVER_TIME = 2
ENEMY_RECOVER_HP = 50
```

---

Dúvidas ou sugestões? Entre em contato! 