export function IteniumDino() {
  return (
    <div className="flex flex-col h-full">
      <h1 className="text-2xl font-bold mb-4">Itenium Dino</h1>
      <iframe
        src="/games/itenium-dino.html"
        className="flex-1 w-full border-0 rounded-lg"
        style={{ minHeight: '600px' }}
        title="Itenium Dino"
      />
    </div>
  );
}
