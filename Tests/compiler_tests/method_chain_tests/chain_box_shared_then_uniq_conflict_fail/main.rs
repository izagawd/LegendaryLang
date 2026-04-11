// b.get_ref() returns &i32 (shared borrow on b). While r lives, b.inc() takes &mut.
// Borrow conflict: shared + mut on same source.
struct Counter { val: i32 }
impl Counter {
    fn get_ref(self: &Self) -> &i32 { &self.val }
    fn inc(self: &mut Self) { self.val = self.val + 1; }
}
fn main() -> i32 {
    let b = Box.New(make Counter { val: 0 });
    let r: &i32 = b.get_ref();
    b.inc();
    *r
}
