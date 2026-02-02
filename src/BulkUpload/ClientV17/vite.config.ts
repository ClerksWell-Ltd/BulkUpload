import { defineConfig } from 'vite';

export default defineConfig({
  publicDir: 'public',
  build: {
    lib: {
      entry: 'src/index.ts',
      formats: ['es'],
      fileName: () => 'bulkupload.js'
    },
    outDir: '../wwwroot',
    emptyOutDir: false, // Don't empty outDir - V13 files may already be there
    rollupOptions: {
      external: [/^@umbraco/]
    },
    sourcemap: true
  }
});
