# Sistema de Evento de EXP

Agora o servidor conta com um sistema de evento de EXP configurável pelo painel de administração!

O administrador pode ativar o evento, definir o multiplicador (ex: 2x, 3x, etc.) e o tempo de duração. Enquanto o evento estiver ativo, todos os jogadores recebem experiência multiplicada ao derrotar monstros, tanto solo quanto em grupo (party). Jogadores VIP acumulam o bônus VIP normalmente junto ao evento. Quando o evento começa, uma mensagem global avisa todos os jogadores do multiplicador e do tempo restante.

**Exemplo:**

> "Evento de EXP ativado! 2x até 14h."

Ideal para finais de semana, eventos especiais ou para animar a comunidade!

---

## No Client

### Em `[VS] Window_Panel`

**Em `def initialize`**

Ajuste a altura da janela, algo nesse sentido já fica bom:

```ruby
super(adjust_x, adjust_y, 385, 330)
```

**No final do método `def create_buttons`, acima do `end`, adicione:**

```ruby
# Hiwatari - campos e botão para evento de exp
@exp_mult_box = Number_Box.new(self, 15, 300, 60, 2)
@exp_mult_box.value = 2
@exp_time_box = Number_Box.new(self, 80, 300, 60, 3)
@exp_time_box.value = 60
@exp_event_button = Button.new(self, 145, 300, 'Evento EXP', 100) { set_exp_event }
```

**Ainda nesse script, no final, abaixo de `def update`, depois do segundo `end`, adicione:**

```ruby
# Hiwatari - método para ativar/desativar evento de exp
def set_exp_event
  mult = @exp_mult_box.value
  time = @exp_time_box.value
  if mult > 1 && time > 0
    $network.send_admin_command(Enums::Command::EXP_EVENT, '', mult, time)
  else
    $network.send_admin_command(Enums::Command::EXP_EVENT, '', 1, 0)
  end
end
```

### Em `[VS] Enums`

No final de `Command`, abaixo de `MSG`, adicione:

```ruby
EXP_EVENT
```

Finalizamos no client, vamos partir para o servidor!

---

## No Servidor

### Crie um novo script em `Scripts/Network/exp_event.rb` e adicione:

```ruby
#==============================================================================
# ** Exp_Event
#------------------------------------------------------------------------------
#  Configuração e controle do evento de experiência global.
#  #Hiwatari - Módulo para manipular evento de EXP
#==============================================================================

module Exp_Event
  @active = false
  @multiplier = 1
  @end_time = nil

  class << self
    attr_accessor :active, :multiplier, :end_time

    def activate(multiplier, duration_minutes)
      @active = true
      @multiplier = multiplier
      @end_time = Time.now + (duration_minutes * 60)
    end

    def deactivate
      @active = false
      @multiplier = 1
      @end_time = nil
    end

    def check_exp_event
      if @active && Time.now > @end_time
        deactivate
        return false
      end
      @active
    end

    def current_multiplier
      check_exp_event ? @multiplier : 1
    end

    def time_left
      return 0 unless @active && @end_time
      [@end_time - Time.now, 0].max.to_i
    end
  end
end
```

---

### Em `Scripts/Combat/game_enemy.rb`

Substitua o método `def treasure` por:

```ruby
def treasure
  if $network.clients[@target.id].in_party?
    # Não converte em inteiro aqui, pois o resultado provisório ainda será multiplicado pelo bônus VIP
    $network.clients[@target.id].party_share($data_enemies[@enemy_id].exp * EXP_BONUS, rand($data_enemies[@enemy_id].gold).to_i * GOLD_BONUS, @enemy_id)
  else
    # Converte eventual resultado decimal do bônus de experiência em inteiro
    # Hiwatari - evento de exp para solo
    mult = Exp_Event.current_multiplier
    exp_base = $data_enemies[@enemy_id].exp * EXP_BONUS * $network.clients[@target.id].vip_exp_bonus
    exp_final = (exp_base * mult).to_i
    $network.clients[@target.id].gain_exp(exp_final)
    # Amount será um número inteiro, ainda que o ouro seja 0 e em razão disso o rand retorne um valor decimal
    $network.clients[@target.id].gain_gold((rand($data_enemies[@enemy_id].gold).to_i * GOLD_BONUS * $network.clients[@target.id].gold_rate * $network.clients[@target.id].vip_gold_bonus).to_i, false, true)
    $network.clients[@target.id].add_kills_count(@enemy_id)
  end
  drop_items
end
```

---

### Em `Scripts/Network/game_commands.rb`

No final do método `def admin_commands`, acima dos dois `end`, adicione:

```ruby
when Enums::Command::EXP_EVENT # Hiwatari - comando de evento de exp
  set_exp_event(client, int1, int2)
```

No mesmo script, abaixo do método `def admin_message`, acima do último `end`, adicione:

```ruby
# Hiwatari - método para ativar/desativar evento de exp
def set_exp_event(client, multiplier, duration_minutes)
  if multiplier > 1 && duration_minutes > 0
    Exp_Event.activate(multiplier, duration_minutes)
    end_hour = (Time.now + duration_minutes * 60).strftime('%H:%M')
    global_chat_message("[EVENTO] EXP x#{multiplier} ativo até #{end_hour}!")
    @log.add(client.group, :blue, "#{client.user} ativou evento de exp x#{multiplier} até #{end_hour}.")
  else
    Exp_Event.deactivate
    global_chat_message("[EVENTO] EXP voltou ao normal.")
    @log.add(client.group, :blue, "#{client.user} desativou evento de exp.")
  end
end
```

---

### Em `Scripts/Party/game_party.rb`

Logo no começo do script, acima de `module Game_Party`, adicione:

```ruby
require_relative '../Network/exp_event' # Hiwatari - evento de exp
```

No mesmo script, substitua o método `def party_share_exp` pelo seguinte:

```ruby
def party_share_exp(exp, enemy_id, party_members)
  # Se o número de membros do grupo é superior à experiência ou o jogador é o único membro do grupo no mapa
  if party_members.size > exp || party_members.size == 1
    # Converte eventual resultado decimal do bônus de experiência em inteiro
    gain_exp((exp * vip_exp_bonus).to_i)
    add_kills_count(enemy_id)
    return
  end
  # Hiwatari - Evento de exp
  mult = Exp_Event.current_multiplier
  if mult > 1
    exp = (exp * mult).to_i
  end
  exp_share = exp / party_members.size + (exp * PARTY_BONUS[party_members.size] / 100)
  dif_exp = exp - (exp / party_members.size) * party_members.size
  party_members.each do |member|
    member.gain_exp(member == self ? (exp_share * member.vip_exp_bonus + dif_exp).to_i : (exp_share * member.vip_exp_bonus).to_i)
    member.add_kills_count(enemy_id)
  end
end
```
