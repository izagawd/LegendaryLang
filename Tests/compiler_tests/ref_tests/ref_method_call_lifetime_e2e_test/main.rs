struct Holder {
    val: i32
}
impl Copy for Holder {}
trait GetVal {
    fn get(self: &Self) -> &i32;
}
impl GetVal for Holder {
    fn get(self: &Holder) -> &i32 {
        &self.val
    }
}
fn main() -> i32 {
    let h = Holder { val = 7 };
    let r = h.get();
    *r
}
