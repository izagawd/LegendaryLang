struct Val { n: i32 }
impl Copy for Val {}
impl Val {
    fn get_const(self: &const Self) -> i32 { self.n }
}
fn main() -> i32 {
    let dd = make Val { n: 42 };
    let r: &const Val = &const dd;
    let sr: &&const Val = &r;
    sr.get_const()
}
