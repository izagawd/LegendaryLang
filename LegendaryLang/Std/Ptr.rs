fn PtrWrite[T:! Sized](dst: *mut u8, val: T) -> *mut T;
fn PtrAsU8[T:! Sized](ptr: *mut T) -> *mut u8;
fn DestructPtr[T:! Sized](ptr: *mut T);
