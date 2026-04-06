trait Producer {
    type Output;
}

trait WantsString: Producer(Output = bool) {}

impl Producer for i32 {
    type Output = i32;
}

impl WantsString for i32 {}

fn main() -> i32 {
    5
}
