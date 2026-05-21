// Simple CSV loader that returns the numeric values from the "oi" column
(function () {
    async function loadOiColumn(filePath) {
        try {
            const resp = await fetch(filePath, { cache: 'no-store' });
            if (!resp.ok) {
                console.error('Lỗi tải file CSV. HTTP Status:', resp.status);
                return [];
            }
            const txt = await resp.text();
            const lines = txt.split(/\r?\n/).filter(l => l.trim().length > 0);
            if (lines.length === 0) return [];

            const header = lines[0].split(',').map(h => h.trim());
            const oiIndex = header.findIndex(h => h.toLowerCase() === 'oi');
            if (oiIndex === -1) return [];

            const values = [];
            for (let i = 1; i < lines.length; i++) {
                const cols = lines[i].split(',');
                const raw = (cols[oiIndex] || '').trim();
                const num = parseFloat(raw);
                if (!Number.isFinite(num)) {
                    continue;
                }
                values.push(num);
            }
            return values;
        } catch (e) {
            console.error('csv_module load error:', e);
            return [];
        }
    }

    // expose on window for use by index.js
    window.csvModule = {
        loadOiColumn
    };
})();