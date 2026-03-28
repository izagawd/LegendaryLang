struct Holder {
    val: i32
}
impl Copy for Holder {}
trait GetRef {
    fn get_ref(self: &Self) -> &i32;
}
impl GetRef for Holder {
    fn get_ref(self: &Holder) -> &i32 {
        &self.val
    }
}
fn main() -> i32 {
    let h = Holder { val = 42 };
    let r = h.get_ref();
    &uniq h;
    *r
}
