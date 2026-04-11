// b.get_ref() → use *r (borrow dies, NLL). Then b.inc() + b.get() chain.
struct Counter { val: i32 }
impl Counter {
    fn get_ref(self: &Self) -> &i32 { &self.val }
    fn inc(self: &mut Self) { self.val = self.val + 1; }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let b = Gc.New(make Counter { val: 41 });
    let old = *b.get_ref();
    b.inc();
    b.get() + old - 41
}
