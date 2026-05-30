window.laylaSettings = {
  storageKey: "layla.settings",

  read() {
    try {
      return JSON.parse(localStorage.getItem(this.storageKey) || "{}");
    } catch {
      return {};
    }
  },

  write(prefs) {
    const current = this.read();
    const next = { ...current, ...prefs };
    localStorage.setItem(this.storageKey, JSON.stringify(next));
    this.apply(next);
    return next;
  },

  apply(prefs) {
    const theme = prefs?.theme === "light" ? "light" : "dark";
    document.documentElement.dataset.theme = theme;
  },

  initialize() {
    const prefs = this.read();
    this.apply(prefs);
    return {
      theme: prefs.theme === "light" ? "light" : "dark",
      fullscreen: Boolean(document.fullscreenElement),
    };
  },

  async setFullscreen(enabled) {
    if (enabled) {
      if (!document.fullscreenElement) {
        await document.documentElement.requestFullscreen();
      }
    } else if (document.fullscreenElement) {
      await document.exitFullscreen();
    }
    return Boolean(document.fullscreenElement);
  },

  isFullscreen() {
    return Boolean(document.fullscreenElement);
  }
};

window.laylaSettings.initialize();

window.laylaSettingsInitialize = () => window.laylaSettings.initialize();
window.laylaSettingsWrite = (prefs) => window.laylaSettings.write(prefs);
window.laylaSettingsSetFullscreen = (enabled) => window.laylaSettings.setFullscreen(enabled);
window.laylaSettingsIsFullscreen = () => window.laylaSettings.isFullscreen();
