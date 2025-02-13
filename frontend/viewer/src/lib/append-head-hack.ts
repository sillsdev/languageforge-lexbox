if (import.meta.env.DEV) {
  //when in dev mode, svelte and vite want to put style sheets in the head
  //however blazor will remove them, so we need to put them in the body instead
  // eslint-disable-next-line @typescript-eslint/unbound-method
  const headerAppend = document.head.appendChild;
  document.head.appendChild = function newAppend<T extends Node>(node: T) {
    //this is used for both svelte and vite imports
    if (node.nodeName === 'STYLE') {
      document.getElementById('svelte-app')?.appendChild(node);
    } else {
      headerAppend.call(document.head, node);
    }
    return node;
  };
}

