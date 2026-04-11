fn PtrWrite[T:! type](dst: *mut u8, val: T) -> *mut T;
fn PtrAsU8[T:! type](ptr: *mut T) -> *mut u8;
fn DestructPtr[T:! type](ptr: *mut T);
