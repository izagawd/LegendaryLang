use Std.Ops.Drop;
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

impl[T:! type] Drop for Wrapper(T) {
    fn Drop(self: &uniq Self) {
        free(self.ptr);
    }
}

struct Foo { val: i32 }
impl Copy for Foo {}

impl Foo {
    fn need_mut(self: &mut Self) -> i32 { self.val }
}

fn main() -> i32 {
    let w = make Wrapper(Foo) { ptr: alloc(Foo) };
    *w.ptr = make Foo { val: 5 };
    w.need_mut()
}
