export function DeployPanic() {
  return (
    <div className="flex flex-col h-full">
      <h1 className="text-2xl font-bold mb-4">Deploy Panic</h1>
      <iframe
        src="/games/deploy-panic.html"
        className="flex-1 w-full border-0 rounded-lg"
        style={{ minHeight: '600px' }}
        title="Deploy Panic"
      />
    </div>
  );
}
