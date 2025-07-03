/*:
 * @target MV
 * @plugindesc Mostra o nome do herói acima da cabeça do personagem
 * @author Hiwatari
 * @version 1.0.0
 * 
 * @param showPlayerName
 * @text Mostrar Nome do Jogador
 * @type boolean
 * @default true
 * @desc Se deve mostrar o nome do jogador principal
 * 
 * @param showFollowerNames
 * @text Mostrar Nomes dos Seguidores
 * @type boolean
 * @default false
 * @desc Se deve mostrar os nomes dos seguidores
 * 
 * @param showEventNames
 * @text Mostrar Nomes dos Eventos
 * @type boolean
 * @default false
 * @desc Se deve mostrar os nomes dos eventos (NPCs)
 * 
 * @param fontSize
 * @text Tamanho da Fonte
 * @type number
 * @min 8
 * @max 32
 * @default 16
 * @desc Tamanho da fonte do nome
 * 
 * @param textColor
 * @text Cor do Texto
 * @type string
 * @default #ffffff
 * @desc Cor do texto em hexadecimal
 * 
 * @param outlineColor
 * @text Cor da Borda
 * @type string
 * @default #000000
 * @desc Cor da borda do texto em hexadecimal
 * 
 * @param yOffset
 * @text Deslocamento Vertical
 * @type number
 * @min -100
 * @max 100
 * @default -48
 * @desc Deslocamento vertical do nome em relação à cabeça do personagem
 * 
 * @param opacity
 * @text Opacidade
 * @type number
 * @min 0
 * @max 255
 * @default 255
 * @desc Opacidade do texto (0-255)
 * 
 * @help
 * ============================================================================
 * NameDisplay para RPG Maker MV.
 * ============================================================================
 * 
 * Este plugin mostra o nome dos personagens acima de suas cabeças, similar
 * a um MMORPG.
 * 
 * Características:
 * - Mostra o nome do herói principal
 * - Opcionalmente mostra nomes de seguidores
 * - Para eventos: mostra nome apenas se começar com $
 * - Texto com borda para melhor visibilidade
 * - Configurável (cor, tamanho, posição)
 * 
 * COMO USAR COM EVENTOS:
 * Para que um evento mostre seu nome, defina o nome do evento começando com $:
 * Exemplo: "$Mercador", "$Guarda", "$NPC_João"
 * O símbolo $ será removido automaticamente na exibição.
 * 
 * ============================================================================
 */

(function() {
    'use strict';
    
    var parameters = PluginManager.parameters('[HW] NameDisplay');
    var showPlayerName = parameters['showPlayerName'] !== 'false';
    var showFollowerNames = parameters['showFollowerNames'] === 'true';
    var showEventNames = parameters['showEventNames'] === 'true';
    var fontSize = Number(parameters['fontSize'] || 16);
    var textColor = parameters['textColor'] || '#ffffff';
    var outlineColor = parameters['outlineColor'] || '#000000';
    var yOffset = Number(parameters['yOffset'] || -48);
    var opacity = Number(parameters['opacity'] || 255);

    // Sprite_NameDisplay
    function Sprite_NameDisplay() {
        this.initialize.apply(this, arguments);
    }

    Sprite_NameDisplay.prototype = Object.create(Sprite.prototype);
    Sprite_NameDisplay.prototype.constructor = Sprite_NameDisplay;

    Sprite_NameDisplay.prototype.initialize = function(character) {
        Sprite.prototype.initialize.call(this);
        this._character = character;
        this._name = '';
        this._nameBitmap = null;
        this.initMembers();
        this.createNameBitmap();
        this.update();
    };

    Sprite_NameDisplay.prototype.initMembers = function() {
        this.anchor.x = 0.5;
        this.anchor.y = 1;
        this.z = 1; // Acima do personagem, mas como filho
    };

    Sprite_NameDisplay.prototype.createNameBitmap = function() {
        this._nameBitmap = new Bitmap(200, 48);
        this._nameBitmap.fontSize = fontSize;
        this.bitmap = this._nameBitmap;
    };

    Sprite_NameDisplay.prototype.update = function() {
        Sprite.prototype.update.call(this);
        this.updateName();
        this.updatePosition();
        this.updateVisibility();
    };

    Sprite_NameDisplay.prototype.updateName = function() {
        var name = this.getCharacterName();
        if (this._name !== name) {
            this._name = name;
            this.redrawName();
        }
    };

    Sprite_NameDisplay.prototype.getCharacterName = function() {
        if (this._character === $gamePlayer) {
            return $gameParty.leader().name();
        } else if (this._character instanceof Game_Follower) {
            var actor = this._character.actor();
            return actor ? actor.name() : '';
        } else if (this._character instanceof Game_Event) {
            var eventName = this._character.event().name || '';
            // Só retorna o nome se começar com $
            if (eventName.startsWith('$')) {
                return eventName.substring(1); // Remove o $ do início
            }
            return '';
        }
        return '';
    };

    Sprite_NameDisplay.prototype.redrawName = function() {
        if (!this._name || this._name === '') {
            this.visible = false;
            return;
        }

        this._nameBitmap.clear();
        this._nameBitmap.fontSize = fontSize;
        
        // Desenha a borda
        this._nameBitmap.textColor = outlineColor;
        this._nameBitmap.drawText(this._name, 1, 1, this._nameBitmap.width, this._nameBitmap.height, 'center');
        this._nameBitmap.drawText(this._name, -1, 1, this._nameBitmap.width, this._nameBitmap.height, 'center');
        this._nameBitmap.drawText(this._name, 1, -1, this._nameBitmap.width, this._nameBitmap.height, 'center');
        this._nameBitmap.drawText(this._name, -1, -1, this._nameBitmap.width, this._nameBitmap.height, 'center');
        
        // Desenha o texto principal
        this._nameBitmap.textColor = textColor;
        this._nameBitmap.drawText(this._name, 0, 0, this._nameBitmap.width, this._nameBitmap.height, 'center');
        
        this.visible = true;
        this.opacity = opacity;
    };

    Sprite_NameDisplay.prototype.updatePosition = function() {
        if (this._character) {
            // Usa coordenadas relativas ao personagem
            this.x = 0; // Centralizado no personagem
            this.y = yOffset; // Acima da cabeça do personagem
        }
    };

    Sprite_NameDisplay.prototype.updateVisibility = function() {
        if (this._character) {
            var isVisible = !this._character.isTransparent();
            
            // Verificação adicional para seguidores
            if (this._character instanceof Game_Follower) {
                isVisible = isVisible && this._character.isVisible();
            }
            
            this.visible = isVisible && this._name && this._name !== '';
        }
    };

    // Sobrescrever Sprite_Character
    var _Sprite_Character_initialize = Sprite_Character.prototype.initialize;
    Sprite_Character.prototype.initialize = function(character) {
        _Sprite_Character_initialize.call(this, character);
        this.createNameDisplay();
    };

    Sprite_Character.prototype.createNameDisplay = function() {
        if (this.shouldShowName()) {
            this._nameDisplay = new Sprite_NameDisplay(this._character);
            this.addChild(this._nameDisplay);
        }
    };

    Sprite_Character.prototype.shouldShowName = function() {
        if (this._character === $gamePlayer) {
            return showPlayerName;
        } else if (this._character instanceof Game_Follower) {
            return showFollowerNames;
        } else if (this._character instanceof Game_Event) {
            var eventName = this._character.event().name || '';
            return showEventNames && eventName.startsWith('$');
        }
        return false;
    };

    var _Sprite_Character_update = Sprite_Character.prototype.update;
    Sprite_Character.prototype.update = function() {
        _Sprite_Character_update.call(this);
        this.updateNameDisplay();
    };

    Sprite_Character.prototype.updateNameDisplay = function() {
        if (this._nameDisplay) {
            this._nameDisplay.update();
        }
    };

    // Sobrescrever Spriteset_Map para recriar sprites de nome quando necessário
    var _Spriteset_Map_createCharacters = Spriteset_Map.prototype.createCharacters;
    Spriteset_Map.prototype.createCharacters = function() {
        _Spriteset_Map_createCharacters.call(this);
        this.updateNameDisplays();
    };

    Spriteset_Map.prototype.updateNameDisplays = function() {
        for (var i = 0; i < this._characterSprites.length; i++) {
            var sprite = this._characterSprites[i];
            if (sprite.shouldShowName && !sprite._nameDisplay) {
                sprite.createNameDisplay();
            }
        }
    };

})(); 