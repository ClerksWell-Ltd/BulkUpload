import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    lib: {
      entry: 'src/index.ts',
      formats: ['es'],
      fileName: () => 'bundle.js'
    },
    outDir: '../App_Plugins/BulkUpload/dist',
    emptyOutDir: true,
    rollupOptions: {
      external: [/^@umbraco/, 'lit']
    },
    sourcemap: true
  }
});
