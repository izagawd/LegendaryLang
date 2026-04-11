struct Wrapper(T:! Sized) {
    ptr: *mut T
}

impl[T:! Sized] Receiver for Wrapper(T) {
    let Target :! type = T;
}

impl[T:! Sized] Deref for Wrapper(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let f = make Foo { val: 42 };
    let w = make Wrapper(Foo) { ptr: &raw mut f };
    w.get()
}
