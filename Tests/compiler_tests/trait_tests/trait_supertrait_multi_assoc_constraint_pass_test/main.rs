trait Transformer {
    let Input :! Sized;
    let Output :! Sized;
}

trait I32Transformer: Transformer(Input = i32, Output = i32) {}

impl Transformer for i32 {
    let Input :! Sized = i32;
    let Output :! Sized = i32;
}

impl I32Transformer for i32 {}

fn main() -> i32 {
    42
}
