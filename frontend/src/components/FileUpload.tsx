import React, { useState } from 'react';
import { api } from '../api';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

interface FileUploadProps {
  onUploadSuccess: () => void;
  compact?: boolean;
}

export const FileUpload: React.FC<FileUploadProps> = ({ onUploadSuccess, compact }) => {
  const [uploading, setUploading] = useState(false);
  const [clearing, setClearing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      setUploading(true);
      setError(null);
      setMessage(null);

      const result = await api.uploadSaveFile(file);
      setMessage(`OK: ${result.message}`);
      onUploadSuccess();

      // Reset input
      event.target.value = '';
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleClearData = async () => {
    if (!window.confirm('Clear all uploaded save data? This cannot be undone.')) {
      return;
    }

    try {
      setClearing(true);
      setError(null);
      setMessage(null);

      const result = await api.clearSaveData();
      setMessage(`OK: ${result.message}`);
      onUploadSuccess();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Clear failed');
    } finally {
      setClearing(false);
    }
  };

  return (
    <Card className={compact ? "border-0 bg-transparent shadow-none" : "border-b-0 shadow-none"}>
      <CardContent className={compact ? "space-y-2 p-0" : "space-y-3 p-4"}>
        <div className={compact ? "flex flex-wrap gap-2" : ""}>
        <Button asChild className={compact ? "" : "w-full"}>
          <label htmlFor="save-file">Scan Folder</label>
        </Button>
      <input
        id="save-file"
        type="file"
        accept=".sav"
        onChange={handleFileChange}
        disabled={uploading}
        className="hidden"
      />
        <Button
          type="button"
          variant="destructive"
          className={compact ? "" : "w-full"}
          onClick={handleClearData}
          disabled={uploading || clearing}
        >
          {clearing ? 'Clearing...' : 'Clear Uploaded Data'}
        </Button>
        </div>
        {uploading && <p className="rounded-md bg-primary/10 px-3 py-2 text-xs text-primary">Uploading...</p>}
        {message && <p className="rounded-md bg-emerald-500/10 px-3 py-2 text-xs text-emerald-400">{message}</p>}
        {error && <p className="rounded-md bg-destructive/10 px-3 py-2 text-xs text-destructive">{error}</p>}
      </CardContent>
    </Card>
  );
};
