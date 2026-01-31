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
    emptyOutDir: true,
    rollupOptions: {
      external: [/^@umbraco/, 'lit']
    },
    sourcemap: true
  }
});
