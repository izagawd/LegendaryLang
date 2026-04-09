trait GetVal {
    fn get(self: &Self) -> i32;
}
impl GetVal for i32 {
    fn get(self: &i32) -> i32 {
        *self
    }
}
fn main() -> i32 {
    let x = 7;
    x.get()
}
