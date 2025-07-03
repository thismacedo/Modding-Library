/*:
 * @target MV
 * @plugindesc Carrega diretamente no mapa.
 * @author Hiwatari
 * @version 1.0.0
 *
 * @param debugEnabled
 * @text Ativar Debug
 * @type boolean
 * @default true
 * @desc Ativa o modo debug que carrega diretamente no mapa
 *
 * @help
 * =============================================================================
 * Carrega Diretamente no Mapa, F12 para visualizar os logs.
 * =============================================================================
 *
 * Este plugin faz o jogo carregar diretamente no mapa especificado
 * nas configurações do RPG Maker MV, pulando completamente a tela de título.
 * Útil para testes rápidos.
 *
 * O jogador aparecerá na posição definida no editor do RPG Maker MV.
 *
 * =============================================================================
 */

(function () {
  "use strict";

  // Parâmetros do plugin
  var parameters = PluginManager.parameters("Debug");
  var debugEnabled = parameters["debugEnabled"] !== "false";

  // Intercepta o carregamento no nível mais baixo possível
  var _SceneManager_initialize = SceneManager.initialize;
  SceneManager.initialize = function () {
    _SceneManager_initialize.call(this);

    if (debugEnabled) {
      this._debugMode = true;
      console.log("=== DEBUG MODE ATIVADO ===");
      console.log(
        "Jogo carregará diretamente no mapa configurado no RPG Maker MV"
      );
    }
  };

  // Sobrescreve o SceneManager para pular a tela de título completamente
  var _SceneManager_run = SceneManager.run;
  SceneManager.run = function (sceneClass) {
    if (debugEnabled && sceneClass === Scene_Title) {
      this.startDebugGame();
    } else {
      _SceneManager_run.call(this, sceneClass);
    }
  };

  // Inicia o jogo diretamente no modo debug
  SceneManager.startDebugGame = function () {
    DataManager.setupNewGame();
    this.goto(Scene_Map);
  };

  // Intercepta a criação da cena de título
  var _SceneManager_createScene = SceneManager.createScene;
  SceneManager.createScene = function () {
    if (debugEnabled && this._sceneClass === Scene_Title) {
      this._sceneClass = Scene_Map;
    }
    return _SceneManager_createScene.call(this);
  };

  // Sobrescreve a cena de título como backup extra
  var _Scene_Title_start = Scene_Title.prototype.start;
  Scene_Title.prototype.start = function () {
    if (debugEnabled) {
      this.startDebugGame();
    } else {
      _Scene_Title_start.call(this);
    }
  };

  // Inicia o jogo diretamente no modo debug (backup)
  Scene_Title.prototype.startDebugGame = function () {
    DataManager.setupNewGame();
    SceneManager.goto(Scene_Map);
  };

  // Sobrescreve a cena de boot para configurar debug
  var _Scene_Boot_start = Scene_Boot.prototype.start;
  Scene_Boot.prototype.start = function () {
    _Scene_Boot_start.call(this);

    if (debugEnabled) {
      this.setupDebugStart();
    }
  };

  // Configura o início do debug
  Scene_Boot.prototype.setupDebugStart = function () {
    this._debugMode = true;
  };

  // Intercepta o main.js para pular a tela de título desde o início
  var _SceneManager_goto = SceneManager.goto;
  SceneManager.goto = function (sceneClass) {
    if (debugEnabled && sceneClass === Scene_Title) {
      _SceneManager_goto.call(this, Scene_Map);
    } else {
      _SceneManager_goto.call(this, sceneClass);
    }
  };

  // Adiciona informações de debug na tela
  var _Scene_Map_update = Scene_Map.prototype.update;
  Scene_Map.prototype.update = function () {
    _Scene_Map_update.call(this);

    if (debugEnabled) {
      this.updateDebugInfo();
    }
  };

  // Atualiza informações de debug
  Scene_Map.prototype.updateDebugInfo = function () {
    // Mostra informações de debug no console periodicamente
    if (this._debugCounter === undefined) {
      this._debugCounter = 0;
    }

    this._debugCounter++;
    if (this._debugCounter >= 300) {
      // A cada 5 segundos (60fps * 5)
      this._debugCounter = 0;
      if ($gameMap && $gamePlayer) {
        console.log(
          "Debug Info - Mapa:",
          $gameMap.mapId(),
          "Posição:",
          $gamePlayer.x + "," + $gamePlayer.y
        );
      }
    }
  };

  // Intercepta o window.onload para configurar debug antes de tudo
  var originalOnload = window.onload;
  window.onload = function () {
    if (debugEnabled) {
      console.log("=== DEBUG MODE CONFIGURADO ===");
    }

    if (originalOnload) {
      originalOnload();
    }
  };
})();
