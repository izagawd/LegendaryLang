// c.set(42) takes &mut Self. Then c.get() takes &Self. NLL: mut borrow dead after set().
struct Counter { val: i32 }
impl Counter {
    fn set(self: &mut Self, v: i32) { self.val = v; }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let c = make Counter { val: 0 };
    c.set(42);
    c.get()
}
