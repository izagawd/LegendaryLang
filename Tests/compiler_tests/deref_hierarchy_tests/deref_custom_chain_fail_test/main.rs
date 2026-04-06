struct Wrapper(T:! type) {
    ptr: *uniq T
}

impl[T:! type] Receiver for Wrapper(T) {
    type Target = T;
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
    fn need_uniq(self: &uniq Self) -> i32 { self.val }
}

fn main() -> i32 {
    let f = make Foo { val: 5 };
    let w = make Wrapper(Foo) { ptr: &raw uniq f };
    w.need_uniq()
}
