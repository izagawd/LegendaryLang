use Std.Ops.Drop;
struct Wrapper(T:! type) {
    ptr: *uniq T
}

impl[T:! type] Receiver for Wrapper(T) {
    let Target :! type = T;
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

struct Num { val: i32 }
impl Copy for Num {}

impl Num {
    fn get(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let inner_ptr = alloc(Num);
    *inner_ptr = make Num { val: 42 };
    let w = make Wrapper(Num) { ptr: inner_ptr };
    let b = Box(Wrapper(Num)).New(w);
    b.get()
}
