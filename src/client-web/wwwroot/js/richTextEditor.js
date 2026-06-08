const editors = new Map();

export function init(editorId, cursorLayerId, dotNetRef, initialHtml, readOnly) {
  const editor = document.getElementById(editorId);
  const cursorLayer = document.getElementById(cursorLayerId);
  if (!editor || !cursorLayer) return;

  editor.innerHTML = normalizeInitialContent(initialHtml || "");
  editor.contentEditable = readOnly ? "false" : "true";

  const state = {
    editor,
    cursorLayer,
    dotNetRef,
    readOnly,
    suppress: false,
    selectedImage: null,
    inputTimer: null,
    cursorTimer: null
  };

  const onInput = () => {
    if (state.suppress) return;
    window.clearTimeout(state.inputTimer);
    state.inputTimer = window.setTimeout(() => notifyChanged(state), 80);
  };

  const onKeyUp = () => notifyCursorSoon(state);
  const onMouseUp = () => notifyCursorSoon(state);
  const onClick = event => handleImageSelection(state, event);

  editor.addEventListener("input", onInput);
  editor.addEventListener("keyup", onKeyUp);
  editor.addEventListener("mouseup", onMouseUp);
  editor.addEventListener("click", onClick);

  editors.set(editorId, { ...state, onInput, onKeyUp, onMouseUp, onClick });
}

export function dispose(editorId, cursorLayerId) {
  const state = editors.get(editorId);
  const editor = document.getElementById(editorId);
  if (state && editor) {
    editor.removeEventListener("input", state.onInput);
    editor.removeEventListener("keyup", state.onKeyUp);
    editor.removeEventListener("mouseup", state.onMouseUp);
    editor.removeEventListener("click", state.onClick);
  }
  editors.delete(editorId);
  const cursorLayer = document.getElementById(cursorLayerId);
  if (cursorLayer) cursorLayer.innerHTML = "";
}

export function setReadOnly(editorId, readOnly) {
  const state = editors.get(editorId);
  const editor = document.getElementById(editorId);
  if (!state || !editor) return;
  state.readOnly = readOnly;
  editor.contentEditable = readOnly ? "false" : "true";
}

export function setHtml(editorId, html) {
  const state = editors.get(editorId);
  const editor = document.getElementById(editorId);
  if (!state || !editor) return;
  state.suppress = true;
  editor.innerHTML = normalizeInitialContent(html || "");
  state.suppress = false;
}

export function getHtml(editorId) {
  return document.getElementById(editorId)?.innerHTML || "";
}

export function getPlainText(editorId) {
  return document.getElementById(editorId)?.innerText || "";
}

export function exec(editorId, command, value) {
  const state = editors.get(editorId);
  if (!state || state.readOnly) return;
  state.editor.focus();
  document.execCommand(command, false, value || null);
  notifyChanged(state);
}

export function insertImage(editorId) {
  const state = editors.get(editorId);
  if (!state || state.readOnly) return;

  const input = document.createElement("input");
  input.type = "file";
  input.accept = "image/png,image/jpeg,image/gif,image/webp";
  input.onchange = () => {
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      state.editor.focus();
      const figure = document.createElement("figure");
      figure.className = "layla-editor-image align-center";
      figure.contentEditable = "false";

      const img = document.createElement("img");
      img.src = reader.result;
      img.alt = file.name;
      img.draggable = true;

      const caption = document.createElement("figcaption");
      caption.contentEditable = "true";
      caption.textContent = "Añade un pie de imagen";

      figure.appendChild(img);
      figure.appendChild(caption);
      insertNodeAtSelection(figure);
      insertNodeAtSelection(document.createElement("p"));
      notifyChanged(state);
    };
    reader.readAsDataURL(file);
  };
  input.click();
}

export function showRemoteCursor(editorId, cursorLayerId, userId, displayName, offset) {
  const editor = document.getElementById(editorId);
  const layer = document.getElementById(cursorLayerId);
  if (!editor || !layer) return;

  const range = rangeFromTextOffset(editor, offset);
  if (!range) return;

  const rect = range.getBoundingClientRect();
  const parent = editor.getBoundingClientRect();
  let marker = layer.querySelector(`[data-user-id="${cssEscape(userId)}"]`);
  if (!marker) {
    marker = document.createElement("div");
    marker.className = "remote-cursor";
    marker.dataset.userId = userId;
    marker.innerHTML = `<span></span>`;
    layer.appendChild(marker);
  }

  marker.style.left = `${rect.left - parent.left}px`;
  marker.style.top = `${rect.top - parent.top}px`;
  marker.style.height = `${Math.max(rect.height, 18)}px`;
  marker.querySelector("span").textContent = displayName || "Colaborador";
}

export function removeRemoteCursor(cursorLayerId, userId) {
  const layer = document.getElementById(cursorLayerId);
  const marker = layer?.querySelector(`[data-user-id="${cssEscape(userId)}"]`);
  marker?.remove();
}

function notifyChanged(state) {
  state.dotNetRef.invokeMethodAsync("OnEditorChanged", state.editor.innerHTML, state.editor.innerText || "");
}

function notifyCursorSoon(state) {
  if (state.readOnly) return;
  window.clearTimeout(state.cursorTimer);
  state.cursorTimer = window.setTimeout(() => {
    state.dotNetRef.invokeMethodAsync("OnCursorChanged", getSelectionTextOffset(state.editor));
  }, 120);
}

function handleImageSelection(state, event) {
  const figure = event.target.closest?.("figure.layla-editor-image");
  state.editor.querySelectorAll("figure.layla-editor-image.selected").forEach(x => x.classList.remove("selected"));
  if (!figure) return;

  figure.classList.add("selected");
  state.selectedImage = figure;
  ensureImageControls(state, figure);
}

function ensureImageControls(state, figure) {
  let controls = figure.querySelector(".image-controls");
  if (!controls) {
    controls = document.createElement("div");
    controls.className = "image-controls";
    controls.innerHTML = `
      <button type="button" data-align="align-left">Izq</button>
      <button type="button" data-align="align-center">Centro</button>
      <button type="button" data-align="align-right">Der</button>
      <button type="button" data-align="align-wide">Ancha</button>
      <button type="button" data-remove="true">Quitar</button>`;
    figure.appendChild(controls);
  }

  controls.onclick = event => {
    const button = event.target.closest("button");
    if (!button) return;
    const align = button.dataset.align;
    if (align) {
      figure.classList.remove("align-left", "align-center", "align-right", "align-wide");
      figure.classList.add(align);
    }
    if (button.dataset.remove) figure.remove();
    notifyChanged(state);
  };
}

function insertNodeAtSelection(node) {
  const selection = window.getSelection();
  if (!selection || selection.rangeCount === 0) return;
  const range = selection.getRangeAt(0);
  range.deleteContents();
  range.insertNode(node);
  range.setStartAfter(node);
  range.setEndAfter(node);
  selection.removeAllRanges();
  selection.addRange(range);
}

function getSelectionTextOffset(root) {
  const selection = window.getSelection();
  if (!selection || selection.rangeCount === 0) return 0;

  const range = selection.getRangeAt(0);
  const preRange = document.createRange();
  preRange.selectNodeContents(root);
  preRange.setEnd(range.endContainer, range.endOffset);
  return preRange.toString().length;
}

function rangeFromTextOffset(root, offset) {
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
  let currentOffset = 0;
  let node;

  while ((node = walker.nextNode())) {
    const nextOffset = currentOffset + node.nodeValue.length;
    if (offset <= nextOffset) {
      const range = document.createRange();
      range.setStart(node, Math.max(0, offset - currentOffset));
      range.collapse(true);
      return range;
    }
    currentOffset = nextOffset;
  }
  return null;
}

function normalizeInitialContent(value) {
  if (!value) return "<p></p>";
  if (value.trimStart().startsWith("{\\rtf")) {
    return textToHtml(rtfToPlainText(value));
  }
  return value.includes("<") ? value : `<p>${escapeHtml(value).replace(/\n/g, "<br>")}</p>`;
}

function textToHtml(value) {
  const paragraphs = (value || "").split(/\n{2,}/).map(x => x.trim()).filter(Boolean);
  if (paragraphs.length === 0) return "<p></p>";
  return paragraphs.map(x => `<p>${escapeHtml(x).replace(/\n/g, "<br>")}</p>`).join("");
}

function rtfToPlainText(value) {
  return value
    .replace(/\r\n/g, "\n")
    .replace(/\\par[d]?/g, "\n")
    .replace(/\\line/g, "\n")
    .replace(/\\'[0-9a-fA-F]{2}/g, match => {
      const code = parseInt(match.slice(2), 16);
      return Number.isNaN(code) ? "" : String.fromCharCode(code);
    })
    .replace(/\\u(-?\d+)\??/g, (_, code) => {
      const numeric = Number(code);
      return Number.isNaN(numeric) ? "" : String.fromCharCode(numeric < 0 ? numeric + 65536 : numeric);
    })
    .replace(/\\[a-zA-Z]+\d* ?/g, "")
    .replace(/[{}]/g, "")
    .replace(/[ \t]+\n/g, "\n")
    .replace(/\n{3,}/g, "\n\n")
    .replace(/[ \t]{2,}/g, " ")
    .trim();
}

function escapeHtml(value) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}

function cssEscape(value) {
  return window.CSS?.escape ? CSS.escape(value) : value.replace(/"/g, '\\"');
}
