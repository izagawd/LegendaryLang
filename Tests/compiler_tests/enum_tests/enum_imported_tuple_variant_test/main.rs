enum Wrapper { Val(i32), Empty }
use Wrapper.Val;
use Wrapper.Empty;

fn main() -> i32 {
    let w = Wrapper.Val(99);
    match w {
        Val(x) => x,
        Empty => 0
    }
}
