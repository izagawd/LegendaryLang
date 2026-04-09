enum Wrapper { Val(i32), Empty }

fn main() -> i32 {
    match Wrapper.Val(42) {
        Val(x) => x,
        Empty => 0
    }
}
