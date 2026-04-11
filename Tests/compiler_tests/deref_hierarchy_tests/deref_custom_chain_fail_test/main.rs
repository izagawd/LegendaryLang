struct Wrapper(T:! type) {
    ptr: *mut T
}

impl[T:! type] Receiver for Wrapper(T) {
    let Target :! type = T;
}

impl[T:! type] Deref for Wrapper(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

impl[T:! type] DerefMut for Wrapper(T) {
    fn deref_mut(self: &mut Self) -> &mut T {
        &mut *self.ptr
    }
}

struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn need_uniq(self: &mut Self) -> i32 { self.val }
}

fn main() -> i32 {
    let f = make Foo { val: 5 };
    let w = make Wrapper(Foo) { ptr: &raw mut f };
    w.need_uniq()
}
