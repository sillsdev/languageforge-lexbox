<script>
	import { user } from '$lib/user';
	import { Drawer } from '@skeletonlabs/skeleton';
</script>

<Drawer position="right">
	<div class="p-4 flex flex-col items-end">
		<h2 class="pb-2">{$user.name}</h2>
		<span class="text-sm opacity-50">{$user.email}</span>

		<hr>

		<nav class="list-nav">
			<ul>
				<li>
					<a href="/">
						<span class="flex-auto">
							Nav Item
						</span>
					</a>
				</li>
			</ul>
		</nav>
	</div>
</Drawer>
