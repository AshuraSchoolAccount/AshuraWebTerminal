const databaseName = 'ashura-terminal';
const databaseVersion = 1;
const fileStore = 'files';
const folderStore = 'folders';
let database;

const output = document.querySelector('#output');
const tree = document.querySelector('#fileTree');
const commandInput = document.querySelector('#commandInput');
const pathInput = document.querySelector('#pathInput');
const editor = document.querySelector('#editor');
const editorStatus = document.querySelector('#editorStatus');

function openDatabase() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(databaseName, databaseVersion);
    request.onupgradeneeded = () => {
      request.result.createObjectStore(fileStore, { keyPath: 'path' });
      request.result.createObjectStore(folderStore, { keyPath: 'path' });
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

function transaction(storeName, mode = 'readonly') {
  return database.transaction(storeName, mode).objectStore(storeName);
}
function requestResult(request) {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}
async function all(storeName) { return requestResult(transaction(storeName).getAll()); }
async function saveFile(path, content) { await requestResult(transaction(fileStore, 'readwrite').put({ path, content })); }
async function loadFile(path) { return requestResult(transaction(fileStore).get(path)); }
async function saveFolder(path) { await requestResult(transaction(folderStore, 'readwrite').put({ path })); }
async function backupWorkspace() {
  const [files, folders] = await Promise.all([all(fileStore), all(folderStore)]);
  return JSON.stringify({ format: 'ashura-terminal-backup', version: 1, files, folders }, null, 2);
}
async function restoreWorkspace(backup) {
  if (!backup || backup.format !== 'ashura-terminal-backup' || backup.version !== 1 ||
      !Array.isArray(backup.files) || !Array.isArray(backup.folders)) {
    throw new Error('That is not a valid Ashura Terminal backup.');
  }
  if (backup.files.some(file => typeof file.path !== 'string' || typeof file.content !== 'string') ||
      backup.folders.some(folder => typeof folder.path !== 'string')) {
    throw new Error('The backup contains invalid file or folder entries.');
  }

  await new Promise((resolve, reject) => {
    const stores = database.transaction([fileStore, folderStore], 'readwrite');
    stores.objectStore(fileStore).clear();
    stores.objectStore(folderStore).clear();
    backup.files.forEach(file => stores.objectStore(fileStore).put(file));
    backup.folders.forEach(folder => stores.objectStore(folderStore).put(folder));
    stores.oncomplete = resolve;
    stores.onerror = () => reject(stores.error);
    stores.onabort = () => reject(stores.error);
  });
  await refreshTree();
}
function downloadWorkspace() {
  backupWorkspace().then(data => {
    const link = document.createElement('a');
    link.href = URL.createObjectURL(new Blob([data], { type: 'application/json' }));
    link.download = `ashura-terminal-backup-${new Date().toISOString().slice(0, 10)}.json`;
    link.click();
    URL.revokeObjectURL(link.href);
    print('Workspace backup downloaded.', 'accent');
  }).catch(error => print(error.message, 'error'));
}

function normalizeFile(value) {
  let path = value.trim().replaceAll('\\', '/');
  if (!path.toLowerCase().endsWith('.at')) path += '.at';
  if (!path || path.startsWith('/') || path.includes('..') || path.split('/').some(part => !part)) throw new Error('Use a relative .at path such as documents/example.at.');
  return path;
}
function normalizeFolder(value) {
  const path = value.trim().replaceAll('\\', '/').replace(/^\/+|\/+$/g, '');
  if (!path || path.includes('..') || path.split('/').some(part => !part)) throw new Error('Use a relative folder name such as documents.');
  return path;
}
async function ensureParents(path) {
  const parts = path.split('/');
  for (let index = 1; index < parts.length; index++) await saveFolder(parts.slice(0, index).join('/'));
}
function parse(input) {
  const tokens = input.match(/(?:[^\s"]+|"[^"]*")+/g) || [];
  if (!tokens.length) return { command: '', args: [] };
  const args = tokens.slice(1).map(token => {
    if (!token.startsWith('-')) throw new Error(`Argument '${token}' must start with '-'.`);
    return token.slice(1).replace(/^"|"$/g, '');
  });
  return { command: tokens[0].toUpperCase(), args };
}
function print(text, className = '') {
  const line = document.createElement('div');
  line.className = className;
  line.textContent = text;
  output.append(line);
  output.scrollTop = output.scrollHeight;
}
async function refreshTree() {
  const [files, folders] = await Promise.all([all(fileStore), all(folderStore)]);
  tree.replaceChildren();
  [...folders.map(item => `${item.path}/`), ...files.map(item => item.path)].sort().forEach(path => {
    const item = document.createElement('button');
    item.className = `tree-item ${path.endsWith('/') ? 'folder' : ''}`;
    item.textContent = path.endsWith('/') ? `▾ ${path}` : `  ${path}`;
    item.onclick = () => path.endsWith('/') ? openFolder(path.slice(0, -1)) : selectFile(path);
    tree.append(item);
  });
  if (!tree.children.length) print('No files yet. Try PRESETFOLDERS or MAKEFILE.');
}
async function selectFile(path) {
  const file = await loadFile(path);
  if (!file) return print(`File '${path}' does not exist.`, 'error');
  pathInput.value = path;
  editor.value = file.content;
  editorStatus.textContent = 'Loaded from IndexedDB';
}
async function openFolder(path) {
  const [files, folders] = await Promise.all([all(fileStore), all(folderStore)]);
  const prefix = `${path}/`;
  const entries = [...folders.map(item => item.path).filter(item => item.startsWith(prefix)).map(item => `${item.slice(prefix.length).split('/')[0]}/`), ...files.map(item => item.path).filter(item => item.startsWith(prefix)).map(item => item.slice(prefix.length).split('/')[0])];
  print(entries.length ? `${path}/: ${[...new Set(entries)].join(', ')}` : `${path}/ is empty.`, 'accent');
}
async function makeFile(path, content, edit = false) {
  path = normalizeFile(path);
  if (content === undefined) content = editor.value;
  await ensureParents(path);
  await saveFile(path, content);
  await refreshTree();
  await selectFile(path);
  print(`${edit ? 'Edited' : 'Saved'} ${path}.`, 'accent');
}
async function run(input) {
  const { command, args } = parse(input);
  if (!command) return;
  switch (command) {
    case 'HELP': print('MAKEFILE, EDITFILE, LOADFILE, LISTFILES, LISTFOLDERS, OPENFOLDER, ADDFOLDER, PRESETFOLDERS, FETCH -IP, CLEAR'); break;
    case 'CLEAR': output.replaceChildren(); break;
    case 'MAKEFILE': if (args.length > 2) throw new Error('MAKEFILE accepts a path and content.'); await makeFile(args[0] || pathInput.value, args[1]); break;
    case 'EDITFILE': if (!args.length) throw new Error('EDITFILE needs a file path.'); await makeFile(args[0], args[1], true); break;
    case 'LOADFILE': await selectFile(normalizeFile(args[0] || pathInput.value)); break;
    case 'LISTFILES': (await all(fileStore)).forEach(file => print(file.path)); break;
    case 'LISTFOLDERS': (await all(folderStore)).forEach(folder => print(`${folder.path}/`)); break;
    case 'OPENFOLDER': if (args.length !== 1) throw new Error('OPENFOLDER needs one folder name.'); await openFolder(normalizeFolder(args[0])); break;
    case 'ADDFOLDER': if (args.length !== 1) throw new Error('ADDFOLDER needs one folder name.'); await ensureParents(normalizeFolder(args[0]) + '/placeholder.at'); await saveFolder(normalizeFolder(args[0])); await refreshTree(); print(`Created folder ${args[0]}.`, 'accent'); break;
    case 'PRESETFOLDERS': await saveFolder('documents'); await saveFolder('code'); await refreshTree(); print('Created folders documents and code.', 'accent'); break;
    case 'FETCH':
      if (args.length !== 1 || args[0].toUpperCase() !== 'IP') throw new Error('Use FETCH -IP.');
      {
        const response = await fetch('https://api.ipify.org');
        if (!response.ok) throw new Error('Could not fetch the public IP.');
        print(`Public IP: ${(await response.text()).trim()}`, 'accent');
      }
      break;
    default: throw new Error(`Unknown command: ${command}`);
  }
}

document.querySelector('#commandForm').onsubmit = async event => {
  event.preventDefault();
  const input = commandInput.value.trim();
  commandInput.value = '';
  print(`> ${input}`);
  try { await run(input); } catch (error) { print(error.message, 'error'); }
};
document.querySelector('#saveButton').onclick = async () => {
  try { await makeFile(pathInput.value, editor.value, Boolean(pathInput.value)); } catch (error) { print(error.message, 'error'); }
};
document.querySelector('#presetButton').onclick = () => run('PRESETFOLDERS');
document.querySelector('#clearButton').onclick = () => output.replaceChildren();
document.querySelector('#downloadButton').onclick = downloadWorkspace;
document.querySelector('#loadButton').onclick = () => document.querySelector('#backupInput').click();
document.querySelector('#backupInput').onchange = async event => {
  const file = event.target.files[0];
  event.target.value = '';
  if (!file) return;
  try {
    await restoreWorkspace(JSON.parse(await file.text()));
    print('Workspace backup loaded.', 'accent');
  } catch (error) { print(error.message, 'error'); }
};

(async () => {
  try {
    database = await openDatabase();
    print('Ashura Terminal ready. Files persist in this browser with IndexedDB.', 'accent');
    await refreshTree();
  } catch (error) { print(`Browser storage unavailable: ${error.message}`, 'error'); }
})();
