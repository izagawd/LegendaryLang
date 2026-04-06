trait HasVal {
    fn Val() -> i32;
}
impl HasVal for i32 {
    fn Val() -> i32 { 42 }
}
fn get_val(T:! HasVal) -> i32 {
    T.Val()
}
fn main() -> i32 {
    get_val(i32)
}
