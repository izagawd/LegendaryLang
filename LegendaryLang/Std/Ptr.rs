fn PtrWrite[T:! Sized](dst: *mut u8, val: T) -> *mut T;
fn PtrAsU8[T:! Sized](ptr: *mut T) -> *mut u8;
fn DestructPtr[T:! Sized](ptr: *mut T);
fn AddrEq[A:! type, B:! type](a: *shared A, b: *shared B) -> bool;
fn GetMetadata[T:! MetaSized](ptr: *shared T) -> (T as MetaSized).Metadata;
