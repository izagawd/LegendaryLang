fn PtrWrite[T:! type](dst: *uniq u8, val: T) -> *uniq T;
fn PtrAsU8[T:! type](ptr: *uniq T) -> *uniq u8;
fn DestructPtr[T:! type](ptr: *uniq T);
