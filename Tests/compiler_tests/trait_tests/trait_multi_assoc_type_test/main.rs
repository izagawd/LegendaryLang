trait Transform {
    type Input;
    type Output;
    fn transform(x: i32) -> i32;
}

impl Transform for i32 {
    type Input = bool;
    type Output = i32;
    fn transform(x: i32) -> i32 {
        x + 1
    }
}

fn main() -> i32 {
    (i32 as Transform).transform(9)
}
