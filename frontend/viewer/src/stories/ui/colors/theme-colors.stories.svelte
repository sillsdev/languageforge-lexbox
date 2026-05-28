<script module lang="ts">
  import {themes} from '$lib/components/ThemePicker.svelte';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});

  const colorTokens = [
    {label: 'Primary', bg: 'bg-primary', fg: 'text-primary-foreground'},
    {label: 'Secondary', bg: 'bg-secondary', fg: 'text-secondary-foreground'},
    {label: 'Accent', bg: 'bg-accent', fg: 'text-accent-foreground'},
    {label: 'Muted', bg: 'bg-muted', fg: 'text-muted-foreground'},
    {label: 'Destructive', bg: 'bg-destructive', fg: 'text-destructive-foreground'},
    {label: 'Card', bg: 'bg-card', fg: 'text-card-foreground'},
    {label: 'Popover', bg: 'bg-popover', fg: 'text-popover-foreground'},
  ];
</script>

<Story name="Theme colors overview">
  {#snippet template()}
    <div class="flex flex-col gap-6 p-4">
      <div>
        <h2 class="text-lg font-bold">Theme Colors Overview</h2>
        <p class="text-sm text-muted-foreground">Light and dark tokens for every theme.</p>
      </div>

      <section class="grid gap-4">
        <h3 class="text-base font-semibold">Primary Contrast Preview</h3>
        <div class="grid gap-4 md:grid-cols-2">
          <div class="grid gap-2">
            <div class="text-sm font-semibold">Light</div>
            <div class="light rounded border p-3 bg-background text-foreground">
              <div class="grid gap-2">
                {#each themes as theme (theme)}
                  <div class="light flex items-center justify-between rounded px-3 py-2 bg-primary text-primary-foreground" data-theme={theme}>
                    <span class="text-xs font-semibold uppercase tracking-wide">{theme}</span>
                    <span class="text-xs opacity-80">Primary</span>
                  </div>
                {/each}
              </div>
            </div>
          </div>
          <div class="grid gap-2">
            <div class="text-sm font-semibold">Dark</div>
            <div class="dark rounded border p-3 bg-background text-foreground">
              <div class="grid gap-2">
                {#each themes as theme (theme)}
                  <div class="dark flex items-center justify-between rounded px-3 py-2 bg-primary text-primary-foreground" data-theme={theme}>
                    <span class="text-xs font-semibold uppercase tracking-wide">{theme}</span>
                    <span class="text-xs opacity-80">Primary</span>
                  </div>
                {/each}
              </div>
            </div>
          </div>
        </div>
      </section>

      <div class="grid gap-8">
        {#each themes as theme (theme)}
          <div class="grid gap-4">
            <h3 class="text-base font-semibold capitalize">{theme}</h3>
            <div class="grid gap-4 md:grid-cols-2">
              <section class="light rounded border p-4" data-theme={theme}>
                <div class="text-sm font-semibold mb-3">Light</div>
                <div class="grid gap-3">
                  {#each colorTokens as token (token)}
                    <div class="flex items-center justify-between rounded border px-3 py-2 {token.bg} {token.fg}">
                      <span class="text-xs font-semibold uppercase tracking-wide">{token.label}</span>
                      <span class="text-xs opacity-80">Aa</span>
                    </div>
                  {/each}
                </div>
                <div class="mt-4 grid gap-3">
                  <div class="rounded border bg-background text-foreground px-3 py-2">
                    <div class="text-xs font-semibold uppercase tracking-wide">Background</div>
                    <div class="text-xs opacity-70">Default foreground on background.</div>
                  </div>
                  <div class="rounded border bg-background text-foreground px-3 py-2">
                    <div class="text-xs font-semibold uppercase tracking-wide">Border + Input</div>
                    <div class="mt-2 flex items-center gap-2">
                      <span class="rounded border border-border px-2 py-1 text-xs">Border</span>
                      <span class="rounded border border-input bg-background px-2 py-1 text-xs">Input</span>
                      <span class="rounded border border-ring px-2 py-1 text-xs">Ring</span>
                    </div>
                  </div>
                </div>
              </section>

              <section class="dark rounded border p-4 bg-background text-foreground" data-theme={theme}>
                <div class="text-sm font-semibold mb-3">Dark</div>
                <div class="grid gap-3">
                  {#each colorTokens as token (token)}
                    <div class="flex items-center justify-between rounded border px-3 py-2 {token.bg} {token.fg}">
                      <span class="text-xs font-semibold uppercase tracking-wide">{token.label}</span>
                      <span class="text-xs opacity-80">Aa</span>
                    </div>
                  {/each}
                </div>
                <div class="mt-4 grid gap-3">
                  <div class="rounded border bg-background text-foreground px-3 py-2">
                    <div class="text-xs font-semibold uppercase tracking-wide">Background</div>
                    <div class="text-xs opacity-70">Default foreground on background.</div>
                  </div>
                  <div class="rounded border bg-background text-foreground px-3 py-2">
                    <div class="text-xs font-semibold uppercase tracking-wide">Border + Input</div>
                    <div class="mt-2 flex items-center gap-2">
                      <span class="rounded border border-border px-2 py-1 text-xs">Border</span>
                      <span class="rounded border border-input bg-background px-2 py-1 text-xs">Input</span>
                      <span class="rounded border border-ring px-2 py-1 text-xs">Ring</span>
                    </div>
                  </div>
                </div>
              </section>
            </div>
          </div>
        {/each}
      </div>
    </div>
  {/snippet}
</Story>
