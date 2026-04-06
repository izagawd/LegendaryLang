trait Transformer {
    type Input;
    type Output;
}

trait I32Transformer: Transformer(Input = i32, Output = i32) {}

impl Transformer for i32 {
    type Input = i32;
    type Output = i32;
}

impl I32Transformer for i32 {}

fn main() -> i32 {
    42
}
