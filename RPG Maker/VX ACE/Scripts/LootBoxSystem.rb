#==============================================================================
# ** LootBoxSystem
#------------------------------------------------------------------------------
#  Feito para: RPG MAKER VX ACE
#  Autor original: Hiwatari
#------------------------------------------------------------------------------
#  Crie um evento simples e use o chamar script: open_loot_box(1)
#==============================================================================

module LootBoxSystem
    KEY_ITEM_ID = 20  # ID da Chave Misteriosa usada para abrir a caixa.
    BOX_ITEM_IDS = {
      1 => 21 # ID da Caixa Misteriosa no banco de dados de itens.
    }
  
    LOOT_BOXES = {
      1 => [ # ID da Caixa
        {item_id: 10, chance: 0.5, type: :item},   # 0.5% chance
        {item_id: 12, chance: 5.0, type: :weapon}, # 5% chance
        {item_id: 15, chance: 94.5, type: :item}   # 94.5% chance
      ]
    }
  end
  
  class Game_Interpreter
    def open_loot_box(box_id)
      key_id  = LootBoxSystem::KEY_ITEM_ID
      box_item_id = LootBoxSystem::BOX_ITEM_IDS[box_id]
  
      unless $game_party.has_item?($data_items[box_item_id])
        $game_message.add("Você não tem a Caixa Misteriosa para abrir.")
        return
      end
  
      unless $game_party.has_item?($data_items[key_id])
        $game_message.add("Você precisa de uma Chave Misteriosa!")
        return
      end
  
      # Remove a chave e a caixa
      $game_party.lose_item($data_items[key_id], 1)
      $game_party.lose_item($data_items[box_item_id], 1)
  
      box = LootBoxSystem::LOOT_BOXES[box_id]
      roll = rand * 100
      sum = 0
      for drop in box
        sum += drop[:chance]
        if roll <= sum
          case drop[:type]
          when :item
            $game_party.gain_item($data_items[drop[:item_id]], 1)
            item_name = $data_items[drop[:item_id]].name
          when :weapon
            $game_party.gain_item($data_weapons[drop[:item_id]], 1)
            item_name = $data_weapons[drop[:item_id]].name
          when :armor
            $game_party.gain_item($data_armors[drop[:item_id]], 1)
            item_name = $data_armors[drop[:item_id]].name
          end
          $game_message.add("Você recebeu: #{item_name}!")
          break
        end
      end
    end
  end