// b.add(42) takes &mut Self, mutates val, returns new val. Then b.get() reads.
// Sequential: mutation then read through GcMut auto-deref.
struct Accum { val: i32 }
impl Accum {
    fn add(self: &mut Self, n: i32) -> i32 { self.val = self.val + n; self.val }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let b = GcMut.New(make Accum { val: 0 });
    b.add(42);
    b.get()
}
