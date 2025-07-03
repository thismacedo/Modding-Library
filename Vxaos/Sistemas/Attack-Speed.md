# Sistema de Velocidade de Ataque - Vxaos

## Visão Geral

O sistema de velocidade de ataque permite definir diferentes velocidades para cada arma, tanto por configurações em código quanto por traços no banco de dados do RPG Maker VX Ace.

Para este sistema, recomendo que utilize a correção de bug que postei há um tempo sobre a direção do personagem ao atacar [clique aqui](https://aldeiarpgbr.forumeiros.com/t15144-bug-fix-atacar-em-direcao-ao-alvo#112023). Não é obrigatório, desde que você saiba o que está fazendo no game_client.

## Implementação Técnica

### 1. Constantes e Configurações

#### Arquivo: `Cliente/[VS] Configs.rb`

```ruby
# Tempo (em segundos) para heróis e inimigos atacarem novamente
# O tempo mínimo para inimigos é 0.8 (800 milissegundos)
ATTACK_TIME = 0.8

# Hiwatari - Velocidades específicas de ataque por arma (em segundos)
# Se uma arma não estiver listada aqui, usa o ATTACK_TIME padrão
WEAPON_ATTACK_SPEEDS = {}
# Exemplos de velocidades de ataque por arma:
# WEAPON_ATTACK_SPEEDS[1] = 0.5   # Espada rápida (500ms)
# WEAPON_ATTACK_SPEEDS[2] = 1.2   # Machado lento (1200ms)
# WEAPON_ATTACK_SPEEDS[31] = 0.6  # Arco (600ms)
# WEAPON_ATTACK_SPEEDS[49] = 0.9  # Cajado (900ms)

# Hiwatari - Exemplos práticos de velocidades de ataque
# Descomente e ajuste conforme necessário para suas armas
WEAPON_ATTACK_SPEEDS[1] = 0.6   # Machado Pequeno - Rápido
WEAPON_ATTACK_SPEEDS[2] = 1.1   # Machado de Guerra - Lento, porém forte
WEAPON_ATTACK_SPEEDS[31] = 0.7  # Arco Curto - Velocidade média
WEAPON_ATTACK_SPEEDS[49] = 0.8  # Cajado de Madeira - Mágico
```

#### Arquivo: `Servidor/Client/game_character.rb`

```ruby
# Hiwatari - Nova constante para velocidade de ataque
FEATURE_ATTACK_SPEED = 65
```

### 2. Métodos de Cálculo

#### Arquivo: `Servidor/Client/game_character.rb`, de preferência abaixo do método party_ability

```ruby
# Hiwatari - Método para calcular o modificador de velocidade de ataque baseado nos traços da arma
def weapon_attack_speed_modifier
  # Procura por traços de velocidade de ataque na arma equipada
  weapon = equips_objects.compact.find { |equip| equip.is_a?(RPG::Weapon) }
  return 1.0 unless weapon # Retorna modificador padrão se não houver arma

  # Soma todos os modificadores de velocidade de ataque da arma
  speed_features = weapon.features.select { |ft| ft.code == FEATURE_ATTACK_SPEED }
  return 1.0 if speed_features.empty? # Retorna modificador padrão se não houver traços de velocidade

  # Calcula o modificador total (multiplicativo)
  modifier = speed_features.inject(1.0) { |total, ft| total * ft.value }
  return [modifier, 0.1].max # Limita o mínimo a 10% da velocidade original
end
```

#### Arquivo: `Servidor/Combat/game_client.rb`

```ruby
# Hiwatari - Método para obter a velocidade de ataque da arma atual
def weapon_attack_speed
  # Primeiro verifica se há configuração específica para esta arma
  base_speed = Configs::WEAPON_ATTACK_SPEEDS[weapon_id] || Configs::ATTACK_TIME

  # Aplica o modificador baseado nos traços da arma
  modifier = weapon_attack_speed_modifier

  # Retorna a velocidade final (base_speed * modifier)
  return base_speed * modifier
end

def attack_normal
  return if restriction == 4
  # Hiwatari - Usa a velocidade de ataque específica da arma
  @weapon_attack_time = Time.now + weapon_attack_speed
  ani_index = $data_weapons[weapon_id].ani_index || @character_index
  $network.maps[@map_id].events.each_value do |event|
    # Se for um evento, inimigo morto ou inimigo vivo fora do alcance
    next if event.dead? || !in_front?(event)
    hit_enemy(event, $data_weapons[weapon_id].animation_id, ani_index, $data_skills[attack_skill_id])
    return
  end
  return unless $network.maps[@map_id].pvp
  return unless $network.maps[@map_id].total_players > 1
  $network.clients.each do |client|
    next if !client&.in_game? || client.map_id != @map_id || !in_front?(client) || client.admin? || protection_level?(client)
    hit_player(client, $data_weapons[weapon_id].animation_id, ani_index, $data_skills[attack_skill_id])
    break
  end
end

def attack_range
  return if restriction == 4
  # Hiwatari - Usa a velocidade de ataque específica da arma
  @weapon_attack_time = Time.now + weapon_attack_speed
  return if Configs::RANGE_WEAPONS[weapon_id][:item_id] > 0 && !has_item?($data_items[Configs::RANGE_WEAPONS[weapon_id][:item_id]])
  return if Configs::RANGE_WEAPONS[weapon_id][:mp_cost] && mp < Configs::RANGE_WEAPONS[weapon_id][:mp_cost]
  target = get_target
  return unless target && in_range?(target, Configs::RANGE_WEAPONS[weapon_id][:range])

  # Hiwatari - Direciona o jogador para o alvo antes de atacar com arma de alcance
  face_target(target)

  lose_item($data_items[Configs::RANGE_WEAPONS[weapon_id][:item_id]], 1) if Configs::RANGE_WEAPONS[weapon_id][:item_id] > 0
  self.mp -= Configs::RANGE_WEAPONS[weapon_id][:mp_cost] if Configs::RANGE_WEAPONS[weapon_id][:mp_cost]
  x, y = max_passage(target)
  $network.send_add_projectile(self, x, y, target, Enums::Projectile::WEAPON, weapon_id)
  return if blocked_passage?(target, x, y)
  ani_index = $data_weapons[weapon_id].ani_index || @character_index
  if @target.type == Enums::Target::PLAYER && valid_target?(target) && $network.maps[@map_id].pvp && !target.admin? && !protection_level?(target)
    hit_player(target, $data_weapons[weapon_id].animation_id, ani_index, $data_skills[attack_skill_id])
  elsif @target.type == Enums::Target::ENEMY && !target.dead?
    hit_enemy(target, $data_weapons[weapon_id].animation_id, ani_index, $data_skills[attack_skill_id])
  end
end
```

#### Arquivo: `Cliente/[VS] Game_Player.rb`, abaixo do método init_basic

```ruby
# Hiwatari - Método para obter a velocidade de ataque da arma atual
def weapon_attack_speed
  # Primeiro verifica se há configuração específica para esta arma
  weapon_id = actor.weapons[0] ? actor.weapons[0].id : 0
  base_speed = Configs::WEAPON_ATTACK_SPEEDS[weapon_id] || Configs::ATTACK_TIME

  # Aplica o modificador baseado nos traços da arma
  modifier = actor.weapon_attack_speed_modifier

  # Retorna a velocidade final (base_speed * modifier)
  return base_speed * modifier
end

def can_attack?
  # Hiwatari - Usa a velocidade de ataque específica da arma
  @weapon_attack_time = Time.now + weapon_attack_speed
  return can_attack_range? if actor.weapons[0] && range_weapon?
  return can_attack_normal?
end
```

#### Arquivo: `Cliente/[VS] Game_Actor.rb`, após o método buff_ids

```ruby
# Hiwatari - Método para calcular o modificador de velocidade de ataque baseado nos traços da arma
def weapon_attack_speed_modifier
  # Procura por traços de velocidade de ataque na arma equipada
  weapon = weapons[0]
  return 1.0 unless weapon # Retorna modificador padrão se não houver arma

  # Soma todos os modificadores de velocidade de ataque da arma
  speed_features = weapon.features.select { |ft| ft.code == 65 } # FEATURE_ATTACK_SPEED
  return 1.0 if speed_features.empty? # Retorna modificador padrão se não houver traços de velocidade

  # Calcula o modificador total (multiplicativo)
  modifier = speed_features.inject(1.0) { |total, ft| total * ft.value }
  return [modifier, 0.1].max # Limita o mínimo a 10% da velocidade original
end
```

A forma mais flexível é usar traços de arma no banco de dados do RPG Maker:

1. Abra o RPG Maker VX Ace
2. Vá em Banco de Dados > Armas
3. Selecione a arma desejada
4. Na aba "Características", adicione um novo traço:
   - **Código:** 65 (FEATURE_ATTACK_SPEED)
   - **ID de Dados:** 0 (não utilizado)
   - **Valor:** Modificador de velocidade (ex: 0.5 = 50% mais rápido, 2.0 = 2x mais lento)

## Sistema Combinado

O sistema funciona de forma combinada:

1. **Velocidade Base:** Obtida das configurações em `WEAPON_ATTACK_SPEEDS` ou usa o `ATTACK_TIME` padrão
2. **Modificador:** Calculado a partir dos traços da arma (FEATURE_ATTACK_SPEED)
3. **Velocidade Final:** `base_speed * modifier`

### Exemplo Prático

Arma: Espada Básica (ID: 1)  
Velocidade Base: 0.6s (600ms)  
Modificador: 0.8x  
Velocidade Final: 0.48s (480ms)  
Efeito: 20.0% mais rápido

Arma: Machado (ID: 2)  
Velocidade Base: 1.1s (1100ms)  
Modificador: 1.3x  
Velocidade Final: 1.43s (1430ms)  
Efeito: 30.0% mais lento

Arma: Arco Curto (ID: 31)  
Velocidade Base: 0.7s (700ms)  
Modificador: 0.9x  
Velocidade Final: 0.63s (630ms)  
Efeito: 10.0% mais rápido

Arma: Cajado de Madeira (ID: 49)  
Velocidade Base: 0.8s (800ms)  
Modificador: 1.0x  
Velocidade Final: 0.8s (800ms)  
Efeito: Sem modificação

Arma: Arma sem configuração (ID: 99)  
Velocidade Base: 0.8s (800ms)  
Modificador: 1.0x  
Velocidade Final: 0.8s (800ms)  
Efeito: Sem modificação

### É isso... Não recomendo reduzir a velocidade abaixo de 500ms, mesmo que funcione, isso previne exploits e mantém o jogo balanceado.

### Bom desenvolvimento a todos! 