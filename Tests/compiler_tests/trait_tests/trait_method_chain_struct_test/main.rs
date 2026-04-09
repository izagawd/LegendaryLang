struct Num {
    val: i32
}
impl Copy for Num {}
trait Inc {
    fn inc(self: &Self) -> Num;
}
trait GetVal {
    fn get(self: &Self) -> i32;
}
impl Inc for Num {
    fn inc(self: &Num) -> Num {
        make Num { val : self.val + 1 }
    }
}
impl GetVal for Num {
    fn get(self: &Num) -> i32 {
        self.val
    }
}
fn main() -> i32 {
    let n = make Num { val : 5 };
    n.inc().get()
}
