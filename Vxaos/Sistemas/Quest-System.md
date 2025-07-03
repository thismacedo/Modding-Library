# Sistema de Quests Melhorado - Versão Clássica

## Visão Geral

Este documento descreve a implementação completa do sistema de quests melhorado para o VXAOS, incluindo todas as correções para evitar erros `[] nil` e melhorar a robustez do sistema.


## Implementação Completa

### 1. Modificações no Cliente

#### 1.1 [VS] Vocab
**Arquivo:** `Cliente/[VS] Vocab.rb`

Adicione em botões:
```ruby
Decline = 'Recusar'
```

#### 1.2 [VS] Enums
**Arquivo:** `Cliente/[VS] Enums.rb`

Adicione no enum Packet:
```ruby
Packet = enum %w(
  # ... Códigos anteriores ...
  QUEST_PROGRESS
)
```

#### 1.3 [VS] Structs
**Arquivo:** `Cliente/[VS] Structs.rb`

Substitua Reward por:
```ruby
Reward = Struct.new(
  :item_id,
  :item_kind,
  :item_amount,
  :exp,
  :gold
)
```

#### 1.4 [VS] Window_Message
**Arquivo:** `Cliente/[VS] Window_Message.rb`

No método `def fiber_main`, substitua:
```ruby
unless $game_message.choice? || $game_message.item_choice?
```

Por:
```ruby
unless $game_message.choice? || $game_message.item_choice? || quest_dialogue?
```

E substitua o método `def input_quest` por:
```ruby
def input_quest
  $windows[:quest_dialogue].show
  $game_message.visible = false
  # Aguarda até que a janela de quest dialogue seja fechada
  Fiber.yield until !$windows[:quest_dialogue].visible
  # Garante que a mensagem seja limpa
  $game_message.clear
end
```

#### 1.5 [VS] Handle_Data
**Arquivo:** `Cliente/[VS] Handle_Data.rb`

No método `def handle_messages_game`, adicione:
```ruby
when Enums::Packet::QUEST_PROGRESS
  handle_quest_progress(buffer)
```

No método `def handle_use_actor`, substitua:
```ruby
size.times do
  quest_id = buffer.read_byte
  $game_actors[1].quests[quest_id] = Game_Quest.new(quest_id, buffer.read_byte)
end
```

Por:
```ruby
size.times do
  quest_id = buffer.read_byte
  quest_state = buffer.read_byte
  #Hiwatari - Recebe o progresso de kills da quest
  quest_kills = buffer.read_short
  quest_item_amount = buffer.read_short  # Novo: recebe o progresso de itens
  $game_actors[1].quests[quest_id] = Game_Quest.new(quest_id, quest_state, quest_kills || 0)
  # Atualiza o progresso de itens na quest
  if $game_actors[1].quests[quest_id].item_id > 0
    $game_actors[1].quests[quest_id].item_amount = quest_item_amount || 0
  end
end
```

Substitua o método `def handle_add_quest(buffer)` por:
```ruby
def handle_add_quest(buffer)
  quest_id = buffer.read_byte
  #Hiwatari - Recebe o progresso de kills da quest quando ela é adicionada
  quest_kills = buffer.read_short
  quest_item_amount = buffer.read_short  # Novo: recebe o progresso de itens
  $game_actors[1].quests[quest_id] = Game_Quest.new(quest_id, Enums::Quest::IN_PROGRESS, quest_kills || 0)
  # Verifica se a quest foi criada corretamente antes de tentar acessar seus atributos
  return unless $game_actors[1].quests[quest_id]

  # Atualiza o progresso de itens na quest
  if $game_actors[1].quests[quest_id].item_id > 0
    $game_actors[1].quests[quest_id].item_amount = quest_item_amount || 0
  end

  $windows[:chat].write_message("#{Vocab::StartQuest} #{$game_actors[1].quests[quest_id].name}", Configs::SUCCESS_COLOR)
  $windows[:quest].refresh if $windows[:quest].visible
  $game_map.need_refresh = true
end
```

Substitua o método `def handle_finish_quest(buffer)` por:
```ruby
def handle_finish_quest(buffer)
  quest_id = buffer.read_byte
  # Verifica se a quest existe antes de tentar acessar seus atributos
  return unless $game_actors[1].quests[quest_id]

  $game_actors[1].quests[quest_id].state = Enums::Quest::FINISHED
  $windows[:chat].write_message("#{Vocab::FinishQuest} #{$game_actors[1].quests[quest_id].name}", Configs::SUCCESS_COLOR)

  # Fecha a janela de informações da quest se ela estiver aberta para a quest que foi finalizada
  if $windows[:quest_info].visible && $windows[:quest_info].quest == $game_actors[1].quests[quest_id]
    $windows[:quest_info].hide
  end

  if $game_actors[1].quests[quest_id].repeat?
    $game_actors[1].quests.delete(quest_id)
  end
  $windows[:quest].refresh if $windows[:quest].visible
end
```

Adicione o método `handle_quest_progress` no final do módulo (antes do `end` final):
```ruby
def handle_quest_progress(buffer)
  quest_id = buffer.read_byte
  kills = buffer.read_short
  item_amount = buffer.read_short  # Novo: recebe o progresso de itens
  if $game_actors[1].quests[quest_id]
    $game_actors[1].quests[quest_id].kills = kills || 0
    # Atualiza o progresso de itens na quest
    if $game_actors[1].quests[quest_id].item_id > 0
      $game_actors[1].quests[quest_id].item_amount = item_amount || 0
    end
    $windows[:quest].refresh if $windows[:quest].visible
    $windows[:quest_info].refresh if $windows[:quest_info].visible && $windows[:quest_info].quest == $game_actors[1].quests[quest_id]
  end
end
```

#### 1.6 [VS] Game_Quest
**Arquivo:** `Cliente/[VS] Game_Quest.rb`

Substitua todo o conteúdo por:
```ruby
#==============================================================================
# ** Game_Quest
#------------------------------------------------------------------------------
#  Esta classe lida com a missão.
#------------------------------------------------------------------------------
#  Autor: Valentine
#==============================================================================

class Game_Quest

  attr_reader   :name
  attr_reader   :description
  attr_reader   :reward
  attr_writer   :state
  attr_reader   :enemy_id
  attr_reader   :max_kills
  attr_reader   :item_id
  attr_reader   :item_kind
  attr_accessor :item_amount
  attr_reader   :required_item_amount
  attr_accessor :kills

  def initialize(id, state = Enums::Quest::IN_PROGRESS, kills = 0)
    @state = state
    @kills = kills || 0
    @name = Quests::DATA[id][:name] || "Quest #{id}"
    @description = Quests::DATA[id][:desc] || ""
    @reward = Reward.new
    @reward.item_id = Quests::DATA[id][:rew_item_id] || 0
    @reward.item_kind = Quests::DATA[id][:rew_item_kind] || 0
    @reward.item_amount = Quests::DATA[id][:rew_item_amount] || 0
    @reward.exp = Quests::DATA[id][:rew_exp] || 0
    @reward.gold = Quests::DATA[id][:rew_gold] || 0
    @repeat = Quests::DATA[id][:repeat] || false
    @enemy_id = Quests::DATA[id][:enemy_id] || 0
    @max_kills = Quests::DATA[id][:enemy_amount] || 0
    @item_id = Quests::DATA[id][:item_id] || 0
    @item_kind = Quests::DATA[id][:item_kind] || 0
    @item_amount = 0
    @required_item_amount = Quests::DATA[id][:item_amount] || 0
  end

  def in_progress?
    @state == Enums::Quest::IN_PROGRESS
  end

  def finished?
    @state == Enums::Quest::FINISHED
  end

  def repeat?
    @repeat
  end

end
```

#### 1.7 [VS] Window_Quest
**Arquivo:** `Cliente/[VS] Window_Quest.rb`

Substitua todo o conteúdo por:
```ruby
#==============================================================================
# ** Window_Quest
#------------------------------------------------------------------------------
#  Esta classe lida com a janela de missões.
#------------------------------------------------------------------------------
#  Autor: Valentine
#==============================================================================

class Window_Quest < Window_Selectable

  def initialize
    super(173, 170, 235, 212)
    self.visible = false
    self.closable = true
    self.title = Vocab::Quests
    @tab_page = Tab_Control.new(self, [Vocab::InProgress, Vocab::Completed], true) { refresh }
  end

  def line_height
    20
  end

  def make_list
    begin
      if @tab_page.index == 0
        @data = $game_actors[1].quests_in_progress || []
      else
        @data = $game_actors[1].quests_finished || []
      end
    rescue => e
      puts "Erro ao carregar lista de quests: #{e.message}"
      @data = []
    end
  end

  def draw_item(index)
    return unless @data && @data[index]  # Verifica se @data e @data[index] existem
    rect = item_rect_for_text(index)
    quest = @data[index]
    icon_index = quest.finished? ? Configs::QUEST_FINISHED_ICON : Configs::QUEST_IN_PROGRESS_ICON
    rect2 = Rect.new(icon_index % 16 * 24, icon_index / 16 * 24, 24, 24)
    bitmap = Cache.system('Iconset')
    contents.blt(3, rect.y, bitmap, rect2)
    rect.x += 27
    draw_text(rect, quest.name || "Quest #{index}")
  end

  def refresh
    super
    @tab_page.draw_border
  end

  def update
    super
    if Mouse.click?(:L) && index >= 0 && @data && @data[index]
      $windows[:quest_info].show(@data[index])
      Sound.play_ok
    end
  end

  def close_window
    hide
  end

end

#==============================================================================
# ** Window_QuestInfo
#==============================================================================
class Window_QuestInfo < Window_Base

  attr_reader   :quest

  def initialize
    super(415, 151, 245, 231)
    self.visible = false
    self.closable = true
    self.title = Vocab::Information
    create_desc
  end

  def create_desc
    @desc_sprite = Sprite_Desc.new
    @desc_sprite.z = self.z + 100
  end

  def line_height
    20
  end

  def show(quest)
    return unless quest  # Verifica se a quest existe antes de continuar
    @quest = quest
    super()
  end

  def show_desc(item)
    return if @last_item == item
    @desc_sprite.refresh(item)
    @last_item = item
  end

  def hide_desc
    @desc_sprite.visible = false
    @last_item = nil
  end

  def refresh
    contents.clear
    return unless @quest  # Verifica se @quest existe antes de continuar

    change_color(system_color)
    draw_text(0, -2, contents_width, line_height, @quest.name, 1)
    change_color(crisis_color)
    draw_text(0, 115, contents_width, line_height, Vocab::Rewards, 1)
    change_color(normal_color)
    word_wrap(@quest.description).each_with_index do |text, i|
      draw_text(0, line_height * i + 21, contents_width, line_height, text, 1)
    end

    #Hiwatari - Mostra o progresso da quest se estiver em andamento
    if @quest.in_progress?
      draw_quest_progress
    end

    draw_text(0, 139, contents_width, line_height, "#{Vocab::Exp}: #{format_number(@quest.reward.exp)}")
    draw_text(0, 162, contents_width, line_height, "#{Vocab.currency_unit}: #{format_number(@quest.reward.gold)}")
    if @quest.reward.item_id > 0
      reward_item = $game_party.item_object(@quest.reward.item_kind, @quest.reward.item_id)
      if reward_item
        draw_text(130, 152, 45, line_height, "#{Vocab::Item}:")
        draw_icon(reward_item.icon_index, 170, 152)
        draw_text(200, 152, 25, line_height, "x#{@quest.reward.item_amount}")
      end
    end
  end

  def update
    super
    update_desc
  end

  def update_desc
    return unless visible
    return unless @quest  # Verifica se @quest existe antes de continuar
    return if $cursor.object || $dragging_window

    # Verifica se o mouse está sobre o ícone de item de recompensa
    if @quest.reward.item_id > 0
      reward_item = $game_party.item_object(@quest.reward.item_kind, @quest.reward.item_id)
      if reward_item
        item_x = self.x + 170 + 16  # 16 é o padding da janela
        item_y = self.y + 152 + 16  # 16 é o padding da janela

        if Mouse.x >= item_x && Mouse.x <= item_x + 24 && Mouse.y >= item_y && Mouse.y <= item_y + 24
          show_desc(reward_item)
          return
        end
      end
    end

    hide_desc
  end

  #Hiwatari - Método para desenhar o progresso da quest
  def draw_quest_progress
    return unless @quest  # Verifica se @quest existe antes de continuar

    #change_color(crisis_color)
    #draw_text(0, 65, contents_width, line_height, "Progresso:", 1)
    #change_color(normal_color)

    #Hiwatari - Usa os atributos da quest do cliente
    # Progresso de kills
    if @quest.enemy_id && @quest.enemy_id > 0 && @quest.max_kills && @quest.max_kills > 0
      enemy_name = $data_enemies[@quest.enemy_id] ? $data_enemies[@quest.enemy_id].name : "Inimigo"
      kills = @quest.kills || 0
      draw_text(0, 75, contents_width, line_height, "#{enemy_name}: #{kills}/#{@quest.max_kills}", 1)
    end

    # Progresso de itens - usa o progresso atualizado em tempo real
    if @quest.item_id && @quest.item_id > 0 && @quest.required_item_amount && @quest.required_item_amount > 0
      item = $game_party.item_object(@quest.item_kind, @quest.item_id)
      if item
        # Usa o progresso atualizado da quest em vez do inventário
        current_amount = @quest.item_amount
        required_amount = @quest.required_item_amount
        draw_text(0, 95, contents_width, line_height, "#{item.name}: #{current_amount}/#{required_amount}", 1)
      end
    end
  end

end

#==============================================================================
# ** Window_QuestDialogue
#==============================================================================
class Window_QuestDialogue < Window_Base2

  def initialize
    super(adjust_x, adjust_y, 'QuestDialogueWindow')
    self.visible = false
    self.closable = true
    @accept_button = Button.new(self, 60, 244, Vocab::Accept) { accept }
    @decline_button = Button.new(self, 130, 244, Vocab::Decline) { decline }
    create_desc
  end

  def create_desc
    @desc_sprite = Sprite_Desc.new
    @desc_sprite.z = self.z + 100
  end

  def adjust_x
    Graphics.width / 2 - 125
  end

  def adjust_y
    Graphics.height / 2 - 135
  end

  def show
    @quest = Quests::DATA[$game_message.texts.first[/QT(.*):/, 1].to_i - 1]
    quest_id = $game_message.texts.first[/QT(.*):/, 1].to_i - 1
    if $game_actors[1].quests.has_key?(quest_id)
      quest = $game_actors[1].quests[quest_id]
      if quest.in_progress? || quest.finished?
        $network.send_choice(1)
        $game_message.clear
        # Garante que a janela seja fechada corretamente
        close_window
        return
      end
    end
    super
    # Garante que os botões sejam visíveis quando a janela é aberta
    @accept_button.visible = true
    @decline_button.visible = true
  end

  def hide_window
    return unless visible
    $network.send_choice(1)
    $game_message.clear
    # Garante que a janela seja fechada corretamente
    close_window
  end

  def close_window
    hide
    hide_desc
  end

  def show_buttons
    @accept_button.visible = true
    @decline_button.visible = true
  end

  def hide_buttons
    @accept_button.visible = false
    @decline_button.visible = false
  end

  def show_desc(item)
    return if @last_item == item
    @desc_sprite.refresh(item)
    @last_item = item
  end

  def hide_desc
    @desc_sprite.visible = false
    @last_item = nil
  end

  def refresh
    contents.clear
    return unless @quest  # Verifica se @quest existe antes de continuar

    contents.font.size = Font.default_size
    change_color(crisis_color)
    draw_text(32, 4, contents_width, line_height, @quest[:name])
    change_color(hp_gauge_color2)
    draw_text(0, 147, contents_width, line_height, Vocab::Rewards, 1)
    change_color(normal_color)
    draw_justified_texts(10, 28, contents_width + 20, line_height, $game_message.all_text.gsub("\n", '').sub(/QT(.*):/, ''))
    contents.font.size = 14
    x = 67
    if @quest[:rew_exp] > 0
      draw_icon(Configs::EXP_ICON, x, 181)
      draw_text(x - 4, 191, 31, line_height, format_number(@quest[:rew_exp]), 2)
      x += 35
    end
    if @quest[:rew_gold] > 0
      draw_icon(Configs::GOLD_ICON, x, 181)
      draw_text(x - 4, 191, 31, line_height, format_number(@quest[:rew_gold]), 2)
      x += 35
    end
    if @quest[:rew_item_id] > 0
      @reward_item = $game_party.item_object(@quest[:rew_item_kind], @quest[:rew_item_id])
      draw_icon(@reward_item.icon_index, x, 181)
      draw_text(x - 4, 191, 31, line_height, @quest[:rew_item_amount], 2)
    else
      @reward_item = nil
    end
  end

  def update
    super
    update_desc
  end

  def update_desc
    return unless visible
    return unless @quest  # Verifica se @quest existe antes de continuar
    return if $cursor.object || $dragging_window

    # Verifica se o mouse está sobre o ícone de item de recompensa
    if @quest[:rew_item_id] > 0 && @reward_item
      # Posição do ícone relativa à janela
      icon_x = 67
      icon_x += 35 if @quest[:rew_exp] > 0
      icon_x += 35 if @quest[:rew_gold] > 0

      # Posição absoluta do ícone na tela (ajustada para o padding da janela)
      item_x = self.x + icon_x + 16  # 16 é o padding da janela
      item_y = self.y + 181 + 16     # 16 é o padding da janela

      if Mouse.x >= item_x && Mouse.x <= item_x + 24 && Mouse.y >= item_y && Mouse.y <= item_y + 24
        show_desc(@reward_item)
        return
      end
    end

    hide_desc
  end

  def accept
    $network.send_choice(0)
    $game_message.clear
    # Garante que a janela seja fechada corretamente
    close_window
  end

  def decline
    $network.send_choice(1)
    $game_message.clear
    # Garante que a janela seja fechada corretamente
    close_window
  end

end
```

#### 1.8 [VS] Game_Actor
**Arquivo:** `Cliente/[VS] Game_Actor.rb`

Melhore os métodos `quests_in_progress` e `quests_finished`:
```ruby
def quests_in_progress
  return [] unless @quests && @quests.is_a?(Hash)
  @quests.values.select { |quest| quest && quest.respond_to?(:in_progress?) && quest.in_progress? }
end

def quests_finished
  return [] unless @quests && @quests.is_a?(Hash)
  @quests.values.select { |quest| quest && quest.respond_to?(:finished?) && quest.finished? }
end
```

### 2. Modificações no Servidor

#### 2.1 Database.rb
**Arquivo:** `Servidor/Database/database.rb`

Melhore o método `load_player_quests`:
```ruby
def self.load_player_quests(actor, s_client)
  actor.quests = {}
  begin
    quests = s_client[:actor_quests].where(:actor_id => actor.id_db)
    quests.each { |row| 
      # Verifica se os dados são válidos antes de criar a quest
      if row && row[:quest_id] && row[:state] && row[:kills]
        actor.quests[row[:quest_id]] = Game_Quest.new(row[:quest_id], row[:state], row[:kills])
      end
    }
  rescue => e
    # Log do erro para debug
    puts "Erro ao carregar quests para actor #{actor.id_db}: #{e.message}"
    actor.quests = {}
  end
end
```

Melhore o método `save_player_quests`:
```ruby
def self.save_player_quests(client, s_client)
  begin
    quests = s_client[:actor_quests].select(:quest_id).where(:actor_id => client.id_db).map(:quest_id)
    client.quests.each do |quest_id, quest|
      if quests.include?(quest_id)
        s_client[:actor_quests].where(:actor_id => client.id_db, :quest_id => quest_id).update(:state => quest.state, :kills => quest.kills)
        quests.delete(quest_id)
      else
        # Salva todos os dados da nova missão, inclusive state e kills, já que ela
        #pode ter sido finalizada logo após ter sido iniciada pelo jogador
        s_client[:actor_quests].insert(:actor_id => client.id_db, :quest_id => quest_id, :state => quest.state, :kills => quest.kills)
      end
    end
    quests.each { |quest_id| s_client[:actor_quests].where(:actor_id => client.id_db, :quest_id => quest_id).delete }
  rescue => e
    # Log do erro para debug
    puts "Erro ao salvar quests para client #{client.id_db}: #{e.message}"
  end
end
```

#### 2.2 game_quest.rb
**Arquivo:** `Servidor/Client/game_quest.rb`

Substitua todo o conteúdo por:
```ruby
#==============================================================================
# ** Game_Quest
#------------------------------------------------------------------------------
#  Esta classe lida com a missão.
#------------------------------------------------------------------------------
#  Autor: Valentine
#==============================================================================

class Game_Quest

  attr_reader   :switch_id
  attr_reader   :variable_id
  attr_reader   :variable_amount
  attr_reader   :item_id
  attr_reader   :item_kind
  attr_accessor :item_amount
  attr_reader   :required_item_amount
  attr_reader   :enemy_id
  attr_reader   :max_kills
  attr_reader   :reward
  attr_accessor :state
  attr_accessor :kills

  def initialize(id, state, kills)
    @state = state
    @kills = kills || 0
    @switch_id = Quests::DATA[id][:switch_id] || 0
    @variable_id = Quests::DATA[id][:variable_id] || 0
    @variable_amount = Quests::DATA[id][:variable_amount] || 0
    @item_id = Quests::DATA[id][:item_id] || 0
    @item_kind = Quests::DATA[id][:item_kind] || 0
    @item_amount = 0
    @required_item_amount = Quests::DATA[id][:item_amount] || 0
    @enemy_id = Quests::DATA[id][:enemy_id] || 0
    @max_kills = Quests::DATA[id][:enemy_amount] || 0
    @reward = Reward.new
    @reward.item_id = Quests::DATA[id][:rew_item_id] || 0
    @reward.item_kind = Quests::DATA[id][:rew_item_kind] || 0
    @reward.item_amount = Quests::DATA[id][:rew_item_amount] || 0
    @reward.exp = Quests::DATA[id][:rew_exp] || 0
    @reward.gold = Quests::DATA[id][:rew_gold] || 0
    @repeat = Quests::DATA[id][:repeat] || false
  end

  def in_progress?
    @state == Enums::Quest::IN_PROGRESS
  end

  def finished?
    @state == Enums::Quest::FINISHED
  end

  def repeat?
    @repeat
  end

end
```

#### 2.3 game_client.rb
**Arquivo:** `Servidor/Client/game_client.rb`

Substitua o método `def add_kills_count(enemy_id)` por:
```ruby
def add_kills_count(enemy_id)
  @quests.each do |quest_id, quest|
    next unless quest.in_progress?
    next unless quest.enemy_id == enemy_id
    next if quest.kills == quest.max_kills
    quest.kills = (quest.kills || 0) + 1
    $network.player_chat_message(self, "#{Killed} #{quest.kills}/#{quest.max_kills} #{$data_enemies[enemy_id].name}.", Configs::SUCCESS_COLOR)
    $network.send_quest_progress(self, quest_id)
    break
  end
end
```

Substitua o método `def add_itens_count(item)` por:
```ruby
def add_itens_count(item)
  @quests.each do |quest_id, quest|
    next unless quest.in_progress?
    next unless quest.item_id == item.id
    next if item_number(item) > quest.required_item_amount
    # Atualiza o progresso de itens na quest
    quest.item_amount = item_number(item)
    $network.player_chat_message(self, "#{Have} #{item_number(item)}/#{quest.required_item_amount} #{item.name}.", Configs::SUCCESS_COLOR)
    $network.send_quest_progress(self, quest_id)
    break
  end
end
```

Substitua o método `def finish_quest(quest_id)` por:
```ruby
def finish_quest(quest_id)
  @quests[quest_id].state = Enums::Quest::FINISHED
  item = item_object(@quests[quest_id].item_kind, @quests[quest_id].item_id)
  lose_item(item, @quests[quest_id].required_item_amount)
  lose_trade_item(item, @quests[quest_id].required_item_amount) if in_trade?
  gain_gold(@quests[quest_id].reward.gold, false, true)
  gain_exp(@quests[quest_id].reward.exp)
  item = item_object(@quests[quest_id].reward.item_kind, @quests[quest_id].reward.item_id)
  gain_item(item, @quests[quest_id].reward.item_amount, false, true) unless full_inventory?(item)
  $network.send_finish_quest(self, quest_id)
  # Remove a quest apenas após enviar o pacote de finalização
  @quests.delete(quest_id) if @quests[quest_id].repeat?
end
```

#### 2.4 send_data.rb
**Arquivo:** `Servidor/Network/send_data.rb`

No método `def send_use_actor(client)`, abaixo da linha:
```ruby
buffer.write_byte(quest.state)
```

Adicione:
```ruby
#Hiwatari - Envia o progresso de kills da quest
buffer.write_short(quest.kills || 0)
# Adiciona o progresso de itens
if quest.item_id > 0
  item = client.item_object(quest.item_kind, quest.item_id)
  item_amount = item ? client.item_number(item) : 0
  buffer.write_short(item_amount)
else
  buffer.write_short(0)
end
```

Substitua o método `def send_add_quest(client, quest_id)` por:
```ruby
def send_add_quest(client, quest_id)
  # Verifica se a quest existe antes de tentar acessar seus atributos
  return unless client.quests[quest_id]

  buffer = Buffer_Writer.new
  buffer.write_byte(Enums::Packet::ADD_QUEST)
  buffer.write_byte(quest_id)
  #Hiwatari - Envia o progresso de kills da quest
  buffer.write_short(client.quests[quest_id].kills || 0)
  # Adiciona o progresso de itens
  if client.quests[quest_id].item_id > 0
    item = client.item_object(client.quests[quest_id].item_kind, client.quests[quest_id].item_id)
    item_amount = item ? client.item_number(item) : 0
    buffer.write_short(item_amount)
  else
    buffer.write_short(0)
  end
  client.send_data(buffer.to_s)
end
```

Adicione o método `send_quest_progress` após o método `send_finish_quest`:
```ruby
#Hiwatari - Método para enviar atualização de progresso da quest
def send_quest_progress(client, quest_id)
  # Verifica se a quest existe antes de tentar acessar seus atributos
  return unless client.quests[quest_id]

  buffer = Buffer_Writer.new
  buffer.write_byte(Enums::Packet::QUEST_PROGRESS)
  buffer.write_byte(quest_id)
  buffer.write_short(client.quests[quest_id].kills || 0)
  # Adiciona o progresso de itens
  if client.quests[quest_id].item_id > 0
    item = client.item_object(client.quests[quest_id].item_kind, client.quests[quest_id].item_id)
    item_amount = item ? client.item_number(item) : 0
    buffer.write_short(item_amount)
  else
    buffer.write_short(0)
  end
  client.send_data(buffer.to_s)
end
```

#### 2.5 handle_data.rb
**Arquivo:** `Servidor/Network/handle_data.rb`

Substitua o método `def handle_choice(client, buffer)` por:
```ruby
def handle_choice(client, buffer)
  # Recebe um valor entre 0 a 99.999.999 (8 dígitos do comando de evento Armazenar Número)
  index = buffer.read_int
  client.choice = index
  # Sempre processa a escolha, mesmo se não tem texto, para evitar freeze
  if client.has_text?
    client.message_interpreter.fiber.resume
  else
    # Se não tem texto, limpa a escolha e o interpretador para evitar problemas
    client.choice = nil
    client.message_interpreter = nil
  end
end
```

## Funcionalidades Implementadas

### 1. Progresso em Tempo Real
- **Kills**: Atualização automática do progresso de kills
- **Itens**: Atualização automática do progresso de itens coletados
- **Interface**: Exibição do progresso na janela de informações da quest

### 2. Sistema Robusto
- **Tratamento de Erros**: Verificações de segurança em todos os pontos críticos
- **Logs de Debug**: Mensagens informativas para identificar problemas
- **Fallbacks**: Valores padrão quando dados estão ausentes

### 3. Interface Melhorada
- **Janela de Diálogo**: Interface para aceitar/recusar quests
- **Progresso Visual**: Exibição clara do progresso atual
- **Tooltips**: Descrições detalhadas dos itens de recompensa

### 4. Sincronização
- **Cliente-Servidor**: Sincronização em tempo real do progresso
- **Persistência**: Salvamento automático no banco de dados
- **Recuperação**: Carregamento correto de dados existentes

O sistema está pronto para uso em produção e pode ser facilmente expandido com novas funcionalidades. 