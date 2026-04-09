// b.inc() takes &uniq Self, returns (). Then b.get() takes &Self.
// NLL: inc's borrow is dead after the statement, so get() is fine.
struct Counter { val: i32 }
impl Counter {
    fn inc(self: &uniq Self) { self.val = self.val + 1; }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let b = Box.New(make Counter { val: 41 });
    b.inc();
    b.get()
}
