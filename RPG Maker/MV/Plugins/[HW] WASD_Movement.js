/*:
 * @target MV
 * @plugindesc Sistema de movimento WASD.
 * @author Hiwatari
 * @version 1.0.0
 *
 * @param wasdEnabled
 * @text Ativar Movimento WASD
 * @type boolean
 * @default true
 * @desc Habilita o sistema de movimento WASD
 *
 * @param movementSpeed
 * @type number
 * @min 1
 * @max 10
 * @default 4
 * @desc Velocidade base do movimento (1-10)
 *
 * @help
 * =============================================================================
 * Sistema de Movimento WASD para RPG Maker MV.
 * =============================================================================
 *
 * Este plugin adiciona suporte para movimento usando as teclas WASD:
 * - W: Mover para cima
 * - A: Mover para esquerda
 * - S: Mover para baixo
 * - D: Mover para direita
 *
 * Recursos:
 * - Velocidade configurável
 * - Compatível com sistema de eventos
 * - Suporte a colisões
 * - Seguidores sincronizados
 *
 * =============================================================================
 */

(function () {
  "use strict";

  // Parâmetros do plugin carregados do metadata
  var parameters = PluginManager.parameters("WASD_Movement");
  var wasdEnabled = parameters["wasdEnabled"] !== "false";
  var movementSpeed = Number(parameters["movementSpeed"]) || 4;

  // Adiciona as teclas WASD ao keyMapper do Input
  var _Input_initialize = Input.initialize;
  Input.initialize = function () {
    _Input_initialize.call(this);
    this._addWASDKeys();
  };

  // Método para adicionar teclas WASD ao keyMapper
  Input._addWASDKeys = function () {
    if (wasdEnabled) {
      this.keyMapper[65] = "wasd_left"; // A
      this.keyMapper[87] = "wasd_up"; // W
      this.keyMapper[83] = "wasd_down"; // S
      this.keyMapper[68] = "wasd_right"; // D
    }
  };

  // Extensão da classe Input para adicionar suporte WASD
  var _Input_signX = Input._signX;
  Input._signX = function () {
    var x = _Input_signX.call(this);
    if (wasdEnabled) {
      if (this.isPressed("wasd_left")) {
        x--;
      }
      if (this.isPressed("wasd_right")) {
        x++;
      }
    }
    return x;
  };

  // Extensão da classe Input para adicionar suporte WASD
  var _Input_signY = Input._signY;
  Input._signY = function () {
    var y = _Input_signY.call(this);
    if (wasdEnabled) {
      if (this.isPressed("wasd_up")) {
        y--;
      }
      if (this.isPressed("wasd_down")) {
        y++;
      }
    }
    return y;
  };

  // Extensão para ajustar velocidade de movimento do jogador
  var _Game_Player_distancePerFrame = Game_Player.prototype.distancePerFrame;
  Game_Player.prototype.distancePerFrame = function () {
    var baseSpeed = _Game_Player_distancePerFrame.call(this);
    // Ajusta velocidade baseada no parâmetro do plugin
    return baseSpeed * (movementSpeed / 4);
  };

  // Extensão para ajustar velocidade de movimento dos seguidores
  var _Game_Follower_distancePerFrame =
    Game_Follower.prototype.distancePerFrame;
  Game_Follower.prototype.distancePerFrame = function () {
    var baseSpeed = _Game_Follower_distancePerFrame.call(this);
    // Ajusta velocidade dos seguidores para sincronizar com o jogador
    return baseSpeed * (movementSpeed / 4);
  };

  // Extensão para ajustar velocidade de movimento de eventos
  var _Game_Event_distancePerFrame = Game_Event.prototype.distancePerFrame;
  Game_Event.prototype.distancePerFrame = function () {
    var baseSpeed = _Game_Event_distancePerFrame.call(this);
    // Ajusta velocidade de eventos para manter consistência
    return baseSpeed * (movementSpeed / 4);
  };

  // Inicializa o plugin quando o jogo carrega
  var _Scene_Boot_start = Scene_Boot.prototype.start;
  Scene_Boot.prototype.start = function () {
    _Scene_Boot_start.call(this);
    if (wasdEnabled) {
      Input._addWASDKeys();
    }
  };

  // Garante que as teclas sejam adicionadas após a inicialização completa
  var _Scene_Boot_isReady = Scene_Boot.prototype.isReady;
  Scene_Boot.prototype.isReady = function () {
    if (!_Scene_Boot_isReady.call(this)) {
      return false;
    }
    if (wasdEnabled && Input.keyMapper) {
      Input._addWASDKeys();
    }
    return true;
  };

  // Adiciona as teclas WASD diretamente ao keyMapper
  if (Input.keyMapper && wasdEnabled) {
    Input.keyMapper[65] = "wasd_left"; // A
    Input.keyMapper[87] = "wasd_up"; // W
    Input.keyMapper[83] = "wasd_down"; // S
    Input.keyMapper[68] = "wasd_right"; // D
  }
})();
